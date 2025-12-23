using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SafeHome.API.Controllers;
using SafeHome.API.DTOs;
using SafeHome.API.Services;
using Xunit;

namespace SafeHome.Tests
{
    public class CascadeDeletionAndAuthTests
    {
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
            await using var db = await TestDatabase.CreateAsync();

            var buildingId = await db.InsertBuildingAsync("HQ");
            var sensor1Id = await db.InsertSensorAsync(buildingId, name: "Temp", type: "Temp");
            var sensor2Id = await db.InsertSensorAsync(buildingId, name: "Smoke", type: "Smoke");
            await db.InsertSensorReadingAsync(sensor1Id, 21.5, new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            await db.InsertSensorReadingAsync(sensor2Id, 80, new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc));
            await db.InsertAlertAsync(sensor1Id, message: "Hot", severity: "High", isResolved: false);
            await db.InsertAlertAsync(sensor2Id, message: "Smoke", severity: "Critical", isResolved: false);
            await db.InsertIncidentAsync(buildingId, type: "Fire", status: "Open");
            await db.InsertIncidentAsync(buildingId, type: "Leak", status: "Resolved");

            var service = new BuildingService(db.ConnectionFactory);
            var deleted = await service.DeleteBuilding(buildingId);

            Assert.True(deleted);

            Assert.Equal(0, await db.GetCountAsync("Buildings"));
            Assert.Equal(0, await db.GetCountAsync("Sensors"));
            Assert.Equal(0, await db.GetCountAsync("SensorReadings"));
            Assert.Equal(0, await db.GetCountAsync("Alerts"));
            Assert.Equal(0, await db.GetCountAsync("Incidents"));
        }

        [Fact]
        public async Task SensorService_DeleteSensor_RemovesReadingsAndAlerts()
        {
            await using var db = await TestDatabase.CreateAsync();

            var buildingId = await db.InsertBuildingAsync("Main");
            var sensorId = await db.InsertSensorAsync(buildingId, name: "Temp", type: "Temp");
            await db.InsertSensorReadingAsync(sensorId, 10, DateTime.UtcNow);
            await db.InsertAlertAsync(sensorId, message: "Alert", severity: "Low", isResolved: false);

            var service = new SensorService(db.ConnectionFactory);
            var deleted = await service.DeleteSensor(sensorId);

            Assert.True(deleted);
            Assert.Equal(0, await db.GetCountAsync("Sensors"));
            Assert.Equal(0, await db.GetCountAsync("SensorReadings"));
            Assert.Equal(0, await db.GetCountAsync("Alerts"));
        }

        [Fact]
        public async Task AuthController_Me_ReturnsUnauthorized_WhenNoUsernameClaim()
        {
            await using var db = await TestDatabase.CreateAsync();
            var controller = new AuthController(db.ConnectionFactory, CreateJwtConfig());

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
            await using var db = await TestDatabase.CreateAsync();
            await db.InsertUserAsync("alice", BCrypt.Net.BCrypt.HashPassword("pass"), "Admin");
            var controller = new AuthController(db.ConnectionFactory, CreateJwtConfig());

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
            await using var db = await TestDatabase.CreateAsync();
            await db.InsertUserAsync("bob", BCrypt.Net.BCrypt.HashPassword("oldpass"), "User");
            var controller = new AuthController(db.ConnectionFactory, CreateJwtConfig());
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
            await using var db = await TestDatabase.CreateAsync();
            var originalHash = BCrypt.Net.BCrypt.HashPassword("oldpass");
            await db.InsertUserAsync("carol", originalHash, "User");
            var controller = new AuthController(db.ConnectionFactory, CreateJwtConfig());
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

            var updatedHash = await db.GetUserPasswordHashAsync("carol");
            Assert.NotNull(updatedHash);
            Assert.NotEqual(originalHash, updatedHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", updatedHash));
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

