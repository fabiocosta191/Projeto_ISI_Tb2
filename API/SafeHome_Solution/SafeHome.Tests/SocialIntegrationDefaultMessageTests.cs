using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.API.Services;
using SafeHome.Data;
using SafeHome.Data.Models;
using Xunit;

namespace SafeHome.Tests
{
    public class SocialIntegrationDefaultMessageTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task SocialIntegrationService_UsesDefaultMessage_WhenRequestMessageEmpty()
        {
            using var context = CreateContext();
            var building = new Building { Id = 1, Name = "HQ" };
            context.Buildings.Add(building);
            context.Incidents.Add(new Incident { Id = 10, BuildingId = 1, Building = building, Type = "Fire", Status = "Open", StartedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var handler = new CapturingHandler();
            var client = new HttpClient(handler);
            var factory = new FakeHttpClientFactory(client);

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/api", Enabled = true }
            });

            var service = new SocialIntegrationService(context, factory, options);

            await service.ShareIncidentAsync(10, new SocialShareRequestDto { Network = "twitter", Message = "   " });

            Assert.NotNull(handler.LastBody);
            Assert.Contains("Atualização do incidente", handler.LastBody);
            Assert.Contains("Fire", handler.LastBody);
            Assert.Contains("HQ", handler.LastBody);
            Assert.Contains("Open", handler.LastBody);
        }

        [Fact]
        public async Task SocialIntegrationService_DoesNotSetAuthorizationHeader_WhenApiKeyMissing()
        {
            using var context = CreateContext();
            var building = new Building { Id = 1, Name = "HQ" };
            context.Buildings.Add(building);
            context.Incidents.Add(new Incident { Id = 10, BuildingId = 1, Building = building, Type = "Fire", Status = "Open", StartedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var handler = new CapturingHandler();
            var client = new HttpClient(handler);
            var factory = new FakeHttpClientFactory(client);

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/api", ApiKey = "", Enabled = true }
            });

            var service = new SocialIntegrationService(context, factory, options);

            await service.ShareIncidentAsync(10, new SocialShareRequestDto { Network = "twitter", Message = "ok" });

            Assert.Null(handler.LastAuthHeader);
        }

        private class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public FakeHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name = "") => _client;
        }

        private class CapturingHandler : HttpMessageHandler
        {
            public string? LastBody { get; private set; }
            public System.Net.Http.Headers.AuthenticationHeaderValue? LastAuthHeader { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastAuthHeader = request.Headers.Authorization;
                if (request.Content != null)
                {
                    LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
