using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SafeHome.API.Controllers;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.API.Services;
using SafeHome.API.Soap;
using SafeHome.Data;
using SafeHome.Data.Models;
using Xunit;

namespace SafeHome.Tests
{
    public class ControllerAndServiceEdgeTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task DataPortabilityService_ExportsOnlyRequestedSensor()
        {
            using var context = CreateContext();
            var building = new Building { Id = 1, Name = "HQ" };
            var temperature = new Sensor { Id = 10, Name = "Temperature", Building = building, BuildingId = 1 };
            var humidity = new Sensor { Id = 11, Name = "Humidity", Building = building, BuildingId = 1 };
            context.Buildings.Add(building);
            context.Sensors.AddRange(temperature, humidity);
            context.SensorReadings.AddRange(
                new SensorReading { Id = 100, SensorId = temperature.Id, Value = 21.5, Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Utc) },
                new SensorReading { Id = 101, SensorId = humidity.Id, Value = 55, Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 12, 0, 0), DateTimeKind.Utc) }
            );
            await context.SaveChangesAsync();

            var service = new DataPortabilityService(context);
            var csv = await service.ExportSensorReadingsCsvAsync(temperature.Id);

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length); // header + one reading
            Assert.Contains("\"Temperature\"", csv);
            Assert.DoesNotContain("Humidity", csv);
        }

        [Fact]
        public async Task ReportingService_ExportAlertsCsv_EscapesQuotesAndOrdersByTimestamp()
        {
            using var context = CreateContext();
            var building = new Building { Id = 1, Name = "Main" };
            var sensor = new Sensor { Id = 2, Name = "Smoke", Building = building, BuildingId = 1 };
            context.Buildings.Add(building);
            context.Sensors.Add(sensor);

            var olderAlert = new Alert
            {
                Id = 1,
                SensorId = sensor.Id,
                Message = "Normal check",
                Severity = "Low",
                Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 8, 0, 0), DateTimeKind.Utc)
            };
            var latestAlert = new Alert
            {
                Id = 2,
                SensorId = sensor.Id,
                Message = "Trigger \"A\" detected",
                Severity = "High",
                Timestamp = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 8, 0, 0), DateTimeKind.Utc)
            };
            context.Alerts.AddRange(olderAlert, latestAlert);
            await context.SaveChangesAsync();

            var service = new ReportingService(context);
            var csv = await service.ExportAlertsCsvAsync();

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(3, lines.Length); // header + two alerts
            Assert.StartsWith($"{latestAlert.Id},", lines[1]);
            Assert.Contains("Trigger ''A'' detected", csv);
        }

        [Fact]
        public async Task SocialIntegrationService_ThrowsWhenNetworkDisabled()
        {
            using var context = CreateContext();
            var building = new Building { Id = 5, Name = "DataCenter" };
            var incident = new Incident { Id = 8, Type = "Power", Status = "Open", Building = building, BuildingId = building.Id };
            context.Buildings.Add(building);
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/api", Enabled = false }
            });

            var httpClient = new HttpClient(new FakeHttpMessageHandler());
            var service = new SocialIntegrationService(context, new FakeHttpClientFactory(httpClient), options);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ShareIncidentAsync(incident.Id, new SocialShareRequestDto { Network = "twitter", Message = "" }));
        }

        [Fact]
        public async Task SocialIntegrationService_ThrowsWhenIncidentMissing()
        {
            using var context = CreateContext();

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "teams", ApiUrl = "https://social.test/api", Enabled = true }
            });

            var httpClient = new HttpClient(new FakeHttpMessageHandler());
            var service = new SocialIntegrationService(context, new FakeHttpClientFactory(httpClient), options);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ShareIncidentAsync(99, new SocialShareRequestDto { Network = "teams", Message = "ping" }));
        }

        [Fact]
        public async Task DataPortabilityController_ReturnsBadRequestForNullBody()
        {
            var controller = new DataPortabilityController(new FakeDataPortabilityService());

            var result = await controller.ImportSensorReadings(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Readings list cannot be null.", badRequest.Value);
        }

        [Fact]
        public async Task DataPortabilityController_UsesSensorSpecificFileName()
        {
            var fakeService = new FakeDataPortabilityService { ExportContent = "Id,Value\n" };
            var controller = new DataPortabilityController(fakeService);

            var result = await controller.ExportSensorReadings(42);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("sensor-42-readings.csv", fileResult.FileDownloadName);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Equal(42, fakeService.RequestedSensorId);
        }

        [Fact]
        public async Task ReportsController_ExportsIncidentsAsCsvFile()
        {
            var fakeService = new FakeReportingService
            {
                IncidentsCsv = "Id,Type\n1,Fire\n"
            };
            var controller = new ReportsController(fakeService);

            var result = await controller.ExportIncidents();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("incidents.csv", fileResult.FileDownloadName);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Contains("Fire", Encoding.UTF8.GetString(fileResult.FileContents));
        }

        [Fact]
        public async Task SensorsController_CreatesAndUpdatesSensor()
        {
            using var context = CreateContext();
            var service = new SensorService(context);
            var controller = new SensorsController(service);

            var createdResult = await controller.PostSensor(new CreateSensorDto
            {
                Name = "Smoke",
                Type = "Temperature",
                BuildingId = 1
            });

            var created = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
            var createdSensor = Assert.IsType<Sensor>(created.Value);
            Assert.True(createdSensor.Id > 0);

            var updateResponse = await controller.PutSensor(createdSensor.Id + 5, new UpdateSensorDto
            {
                Name = "Smoke",
                Type = "Temperature",
                BuildingId = 1,
                IsActive = true
            });

            var badRequest = Assert.IsType<BadRequestObjectResult>(updateResponse);
            Assert.Contains("Erro ao atualizar", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task SensorReadingsController_StoresAndReadsBack()
        {
            using var context = CreateContext();
            context.Sensors.Add(new Sensor { Id = 77, Name = "Humidity", BuildingId = 1 });
            await context.SaveChangesAsync();

            var service = new SensorReadingService(context);
            var controller = new SensorReadingsController(service);

            var createdResult = await controller.PostReading(new SensorReading
            {
                SensorId = 77,
                Value = 55.5,
                Timestamp = DateTime.SpecifyKind(new DateTime(2024, 2, 2, 10, 0, 0), DateTimeKind.Utc)
            });

            var created = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
            var createdReading = Assert.IsType<SensorReading>(created.Value);
            Assert.True(createdReading.Id > 0);

            var fetched = await controller.GetReading(createdReading.Id);
            var fetchedReading = Assert.IsType<SensorReading>(fetched.Value);
            Assert.Equal(55.5, fetchedReading.Value);

            var notFound = await controller.GetReading(999);
            Assert.IsType<NotFoundResult>(notFound.Result);
        }

        [Fact]
        public async Task AlertsController_HandlesCrudBoundaries()
        {
            using var context = CreateContext();
            context.Sensors.Add(new Sensor { Id = 5, Name = "Temp", BuildingId = 1 });
            await context.SaveChangesAsync();

            var service = new AlertService(context);
            var controller = new AlertsController(service);

            var createdResult = await controller.PostAlert(new Alert
            {
                Message = "Heat",
                Severity = "High",
                SensorId = 5,
                Timestamp = DateTime.SpecifyKind(new DateTime(2024, 3, 1, 9, 0, 0), DateTimeKind.Utc)
            });

            var created = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
            var createdAlert = Assert.IsType<Alert>(created.Value);
            Assert.True(createdAlert.Id > 0);

            var updated = await controller.PutAlert(999, new Alert { Message = "Update" });
            Assert.IsType<NotFoundResult>(updated);

            var deleted = await controller.DeleteAlert(999);
            Assert.IsType<NotFoundResult>(deleted);
        }

        [Fact]
        public async Task IncidentsController_ReportsNotFoundOnUnknownIds()
        {
            using var context = CreateContext();
            context.Buildings.Add(new Building { Id = 1, Name = "HQ" });
            await context.SaveChangesAsync();

            var service = new IncidentService(context);
            var controller = new IncidentsController(service);

            var notFoundUpdate = await controller.PutIncident(999, new Incident { Id = 999, Type = "Fire" });
            Assert.IsType<NotFoundResult>(notFoundUpdate);

            var notFoundDelete = await controller.DeleteIncident(999);
            Assert.IsType<NotFoundResult>(notFoundDelete);
        }

        [Fact]
        public async Task BuildingsController_MismatchedIdsReturnBadRequest()
        {
            using var context = CreateContext();
            var buildingService = new BuildingService(context);
            var weatherService = new FakeWeatherService();
            var controller = new BuildingsController(buildingService, weatherService);

            var response = await controller.PutBuilding(10, new Building { Id = 5, Name = "East" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(response);
            Assert.Contains("não corresponde", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task BuildingsController_ReturnsWeatherAlongsideBuilding()
        {
            using var context = CreateContext();
            var building = new Building { Id = 2, Name = "North", Latitude = 10, Longitude = 20 };
            context.Buildings.Add(building);
            await context.SaveChangesAsync();

            var buildingService = new BuildingService(context);
            var weatherService = new FakeWeatherService();
            var controller = new BuildingsController(buildingService, weatherService);

            var response = await controller.GetBuilding(2);
            var okResult = Assert.IsType<ActionResult<object>>(response);
            var payload = okResult.Value;
            Assert.NotNull(payload);
            dynamic data = payload!;

            Assert.Equal("North", ((Building)data.DadosEdificio).Name);
            Assert.Equal(25, ((WeatherDto)data.MeteorologiaAtual).Temperature);
        }

        [Fact]
        public async Task UsersController_RejectsDuplicateUsernames()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Username = "alice", PasswordHash = "hash", Role = "Admin" });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var controller = new UsersController(service);

            var result = await controller.PostUser(new CreateUserDto { Username = "alice", Password = "123", Role = "User" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("already exists", badRequest.Value?.ToString());
        }

        private class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public FakeHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name = "") => _client;
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            }
        }

        private class FakeReportingService : IReportingService
        {
            public string AlertsCsv { get; set; } = string.Empty;
            public string IncidentsCsv { get; set; } = string.Empty;
            public DashboardSnapshotDto Snapshot { get; set; } = new();

            public Task<string> ExportAlertsCsvAsync() => Task.FromResult(AlertsCsv);

            public Task<string> ExportIncidentsCsvAsync() => Task.FromResult(IncidentsCsv);

            public Task<DashboardSnapshotDto> GetDashboardAsync() => Task.FromResult(Snapshot);
        }

        private class FakeDataPortabilityService : IDataPortabilityService
        {
            public string ExportContent { get; set; } = string.Empty;
            public int? RequestedSensorId { get; private set; }

            public Task<string> ExportSensorReadingsCsvAsync(int? sensorId = null)
            {
                RequestedSensorId = sensorId;
                return Task.FromResult(ExportContent);
            }

            public Task<ImportSummaryDto> ImportSensorReadingsAsync(IEnumerable<SensorReadingImportDto> readings)
            {
                return Task.FromResult(new ImportSummaryDto());
            }
        }

        private class FakeWeatherService : IWeatherService
        {
            public Task<WeatherDto> GetCurrentWeather(double latitude, double longitude)
            {
                return Task.FromResult(new WeatherDto
                {
                    Temperature = 25,
                    Humidity = 60,
                    Description = "Sunny"
                });
            }
        }
    }
}
