using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SafeHome.API.Controllers;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.API.Services;
using Xunit;

namespace SafeHome.Tests
{
    public class AdditionalServiceTests
    {
        [Fact]
        public async Task ReportingService_ReturnsAggregatedSnapshot()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            var sensorId = await db.InsertSensorAsync(buildingId, name: "Smoke", type: "Smoke", isActive: true);
            await db.InsertIncidentAsync(buildingId, type: "Fire", status: "Open");
            await db.InsertIncidentAsync(buildingId, type: "Leak", status: "Resolved");
            await db.InsertAlertAsync(sensorId, message: "Temp high", severity: "High", isResolved: false);

            var service = new ReportingService(db.ConnectionFactory);
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
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            var sensorId = await db.InsertSensorAsync(buildingId, name: "Temperature", type: "Temperature", isActive: true);

            var service = new DataPortabilityService(db.ConnectionFactory);
            var summary = await service.ImportSensorReadingsAsync(new[]
            {
                new SensorReadingImportDto { SensorId = sensorId, Value = 20.5 },
                new SensorReadingImportDto { SensorId = 99, Value = 30.1 },
            });

            Assert.Equal(1, summary.Imported);
            Assert.Equal(1, summary.Skipped);
            Assert.Single(summary.Notes);
            Assert.Equal(1, await db.GetCountAsync("SensorReadings"));
        }

        [Fact]
        public async Task SocialIntegrationService_UsesConfiguredNetwork()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("Data Center");
            var incidentId = await db.InsertIncidentAsync(buildingId, type: "Power", status: "Open");

            var networkOptions = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/share", ApiKey = "abc", Enabled = true }
            });

            var fakeHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://social.test") };
            var httpFactory = new FakeHttpClientFactory(httpClient);

            var service = new SocialIntegrationService(db.ConnectionFactory, httpFactory, networkOptions);
            var result = await service.ShareIncidentAsync(incidentId, new SocialShareRequestDto { Network = "twitter", Message = "custom" });

            Assert.Equal(200, result.ExternalStatusCode);
            Assert.Contains("custom", result.PayloadPreview);
            Assert.Contains("Data Center", result.PayloadPreview);
            Assert.Equal("https://social.test/share", fakeHandler.RequestedUri?.ToString());
            Assert.Equal("Bearer", fakeHandler.AuthorizationHeader?.Scheme);
        }

        [Fact]
        public async Task AuthController_RejectsInvalidRole()
        {
            await using var db = await TestDatabase.CreateAsync();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Jwt:Key", "testkeytestkeytestkey"},
                    {"Jwt:Issuer", "issuer"},
                    {"Jwt:Audience", "aud"}
                })
                .Build();

            var controller = new AuthController(db.ConnectionFactory, configuration);
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
