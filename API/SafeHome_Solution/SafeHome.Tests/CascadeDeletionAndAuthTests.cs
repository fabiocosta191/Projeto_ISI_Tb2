using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SafeHome.API.Controllers;
using SafeHome.API.DTOs;
using SafeHome.API.Services;
using SafeHome.Data;
using SafeHome.Data.Models;
using Xunit;

namespace SafeHome.Tests
{
    public class CascadeDeletionAndAuthTests
    {
        private static AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        private static IConfiguration CreateJwtConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "testkeytestkeytestkeytestkey" },
                    { "Jwt:Issuer", "issuer" },
                    { "Jwt:Audience", "aud" },
                })
                .Build();
        }

        [Fact]
        public async Task BuildingService_DeleteBuilding_RemovesIncidentsSensorsReadingsAndAlerts()
        {
            using var context = CreateContext();

            var building = new Building { Id = 1, Name = "HQ" };
            var s1 = new Sensor { Id = 10, Name = "Temp", BuildingId = 1, Building = building };
            var s2 = new Sensor { Id = 11, Name = "Smoke", BuildingId = 1, Building = building };

            context.Buildings.Add(building);
            context.Sensors.AddRange(s1, s2);

            context.SensorReadings.AddRange(
                new SensorReading { Id = 100, SensorId = 10, Value = 21.5, Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Utc) },
                new SensorReading { Id = 101, SensorId = 11, Value = 80, Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 12, 0, 0), DateTimeKind.Utc) }
            );

            context.Alerts.AddRange(
                new Alert { Id = 200, SensorId = 10, Message = "Hot", Severity = "High", IsResolved = false, Timestamp = DateTime.UtcNow },
                new Alert { Id = 201, SensorId = 11, Message = "Smoke", Severity = "Critical", IsResolved = false, Timestamp = DateTime.UtcNow }
            );

            context.Incidents.AddRange(
                new Incident { Id = 300, BuildingId = 1, Building = building, Type = "Fire", Status = "Open", StartedAt = DateTime.UtcNow },
                new Incident { Id = 301, BuildingId = 1, Building = building, Type = "Leak", Status = "Resolved", StartedAt = DateTime.UtcNow }
            );

            await context.SaveChangesAsync();

            var service = new BuildingService(context);
            var deleted = await service.DeleteBuilding(1);

            Assert.True(deleted);

            Assert.Empty(context.Buildings);
            Assert.Empty(context.Sensors);
            Assert.Empty(context.SensorReadings);
            Assert.Empty(context.Alerts);
            Assert.Empty(context.Incidents);
        }

        [Fact]
        public async Task SensorService_DeleteSensor_RemovesReadingsAndAlerts()
        {
            using var context = CreateContext();

            var building = new Building { Id = 1, Name = "Main" };
            var sensor = new Sensor { Id = 5, Name = "Temp", BuildingId = 1, Building = building };

            context.Buildings.Add(building);
            context.Sensors.Add(sensor);

            context.SensorReadings.Add(new SensorReading
            {
                Id = 1,
                SensorId = 5,
                Value = 10,
                Timestamp = DateTime.UtcNow
            });

            context.Alerts.Add(new Alert
            {
                Id = 2,
                SensorId = 5,
                Message = "Alert",
                Severity = "Low",
                IsResolved = false,
                Timestamp = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new SensorService(context);
            var deleted = await service.DeleteSensor(5);

            Assert.True(deleted);
            Assert.Empty(context.Sensors);
            Assert.Empty(context.SensorReadings);
            Assert.Empty(context.Alerts);
        }

        [Fact]
        public async Task AuthController_Me_ReturnsUnauthorized_WhenNoUsernameClaim()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateJwtConfig());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // sem ClaimTypes.Name
                }
            };

            var response = await controller.GetCurrentUser();
            Assert.IsType<UnauthorizedResult>(response.Result);
        }

        [Fact]
        public async Task AuthController_Me_ReturnsOk_WhenUserExists()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Username = "alice", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), Role = "Admin" });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, CreateJwtConfig());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "Test"))
                }
            };

            var response = await controller.GetCurrentUser();

            var ok = Assert.IsType<OkObjectResult>(response.Result);
            Assert.NotNull(ok.Value);

            // evita binding frágil: só garante que tem Username e Role
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.Contains("alice", json);
            Assert.Contains("Admin", json);
        }

        [Fact]
        public async Task AuthController_ChangePassword_ReturnsBadRequest_WhenCurrentPasswordWrong()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Username = "bob", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"), Role = "User" });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, CreateJwtConfig());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") }, "Test"))
                }
            };

            var result = await controller.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "WRONG",
                NewPassword = "newpass"
            });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("password atual", bad.Value?.ToString());
        }

        [Fact]
        public async Task AuthController_ChangePassword_UpdatesHash_WhenCurrentPasswordCorrect()
        {
            using var context = CreateContext();
            var originalHash = BCrypt.Net.BCrypt.HashPassword("oldpass");
            context.Users.Add(new User { Username = "carol", PasswordHash = originalHash, Role = "User" });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, CreateJwtConfig());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "carol") }, "Test"))
                }
            };

            var result = await controller.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "oldpass",
                NewPassword = "newpass"
            });

            Assert.IsType<OkObjectResult>(result);

            var user = await context.Users.FirstAsync(u => u.Username == "carol");
            Assert.NotEqual(originalHash, user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", user.PasswordHash));
        }

        [Fact]
        public async Task ReportsController_Dashboard_ReturnsOk()
        {
            var fake = new FakeReportingService();
            var controller = new ReportsController(fake);

            var result = await controller.GetDashboard();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ReportsController_ExportAlerts_ReturnsCsvFile()
        {
            var fake = new FakeReportingService
            {
                AlertsCsv = "Id,Message\n1,Test\n"
            };

            var controller = new ReportsController(fake);

            var result = await controller.ExportAlerts();

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("alerts.csv", file.FileDownloadName);
            Assert.Equal("text/csv", file.ContentType);
            Assert.Contains("Test", System.Text.Encoding.UTF8.GetString(file.FileContents));
        }

        [Fact]
        public async Task DataPortabilityController_ExportWithoutSensorId_UsesGenericFileName()
        {
            var fake = new FakeDataPortabilityService { ExportContent = "Id,Value\n" };
            var controller = new DataPortabilityController(fake);

            var result = await controller.ExportSensorReadings(null);

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("sensor-readings.csv", file.FileDownloadName);
        }

        private class FakeReportingService : IReportingService
        {
            public string AlertsCsv { get; set; } = string.Empty;
            public string IncidentsCsv { get; set; } = string.Empty;

            public Task<DashboardSnapshotDto> GetDashboardAsync() => Task.FromResult(new DashboardSnapshotDto());
            public Task<string> ExportAlertsCsvAsync() => Task.FromResult(AlertsCsv);
            public Task<string> ExportIncidentsCsvAsync() => Task.FromResult(IncidentsCsv);
        }

        private class FakeDataPortabilityService : IDataPortabilityService
        {
            public string ExportContent { get; set; } = string.Empty;

            public Task<string> ExportSensorReadingsCsvAsync(int? sensorId = null)
                => Task.FromResult(ExportContent);

            public Task<ImportSummaryDto> ImportSensorReadingsAsync(IEnumerable<SensorReadingImportDto> readings)
                => Task.FromResult(new ImportSummaryDto());
        }
    }
}
