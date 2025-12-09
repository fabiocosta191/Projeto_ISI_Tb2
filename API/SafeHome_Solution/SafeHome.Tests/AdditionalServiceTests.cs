using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public class AdditionalServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task ReportingService_ReturnsAggregatedSnapshot()
        {
            using var context = CreateContext();
            var building = new Building { Name = "HQ" };
            context.Buildings.Add(building);
            context.Sensors.Add(new Sensor { Name = "Smoke", Building = building, IsActive = true });
            context.Incidents.Add(new Incident { Type = "Fire", Status = "Open", Building = building, StartedAt = DateTime.UtcNow });
            context.Incidents.Add(new Incident { Type = "Leak", Status = "Resolved", Building = building, StartedAt = DateTime.UtcNow });
            context.Alerts.Add(new Alert { Message = "Temp high", Severity = "High", SensorId = 1, IsResolved = false, Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ReportingService(context);
            var snapshot = await service.GetDashboardAsync();

            Assert.Equal(1, snapshot.TotalBuildings);
            Assert.Equal(1, snapshot.TotalSensors);
            Assert.Equal(1, snapshot.ActiveSensors);
            Assert.Equal(1, snapshot.OpenIncidents);
            Assert.Equal(1, snapshot.ResolvedIncidents);
            Assert.Equal(1, snapshot.OpenAlerts);
        }

        [Fact]
        public async Task DataPortabilityService_ImportsAndSkipsMissingSensors()
        {
            using var context = CreateContext();
            context.Sensors.Add(new Sensor { Id = 5, Name = "Temperature", BuildingId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new DataPortabilityService(context);
            var summary = await service.ImportSensorReadingsAsync(new[]
            {
                new SensorReadingImportDto { SensorId = 5, Value = 20.5 },
                new SensorReadingImportDto { SensorId = 99, Value = 30.1 },
            });

            Assert.Equal(1, summary.Imported);
            Assert.Equal(1, summary.Skipped);
            Assert.Single(summary.Notes);
            Assert.Equal(1, context.SensorReadings.Count());
        }

        [Fact]
        public async Task SocialIntegrationService_UsesConfiguredNetwork()
        {
            using var context = CreateContext();
            var building = new Building { Id = 10, Name = "Data Center" };
            context.Buildings.Add(building);
            context.Incidents.Add(new Incident { Id = 3, Type = "Power", Status = "Open", Building = building, StartedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var networkOptions = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/share", ApiKey = "abc", Enabled = true }
            });

            var fakeHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://social.test") };
            var httpFactory = new FakeHttpClientFactory(httpClient);

            var service = new SocialIntegrationService(context, httpFactory, networkOptions);
            var result = await service.ShareIncidentAsync(3, new SocialShareRequestDto { Network = "twitter", Message = "custom" });

            Assert.Equal(200, result.ExternalStatusCode);
            Assert.Contains("Power", result.PayloadPreview);
            Assert.Equal("https://social.test/share", fakeHandler.RequestedUri?.ToString());
            Assert.Equal("Bearer", fakeHandler.AuthorizationHeader?.Scheme);
        }

        [Fact]
        public async Task AuthController_RejectsInvalidRole()
        {
            using var context = CreateContext();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Jwt:Key", "testkeytestkeytestkey"},
                    {"Jwt:Issuer", "issuer"},
                    {"Jwt:Audience", "aud"}
                })
                .Build();

            var controller = new AuthController(context, configuration);
            var response = await controller.Register(new RegisterDto { Username = "bob", Password = "123456", Role = "Guest" });

            Assert.IsType<BadRequestObjectResult>(response.Result);
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
            public Uri? RequestedUri { get; private set; }
            public AuthenticationHeaderValue? AuthorizationHeader { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestedUri = request.RequestUri;
                AuthorizationHeader = request.Headers.Authorization;

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { status = "ok" }))
                };

                return Task.FromResult(response);
            }
        }
    }
}
