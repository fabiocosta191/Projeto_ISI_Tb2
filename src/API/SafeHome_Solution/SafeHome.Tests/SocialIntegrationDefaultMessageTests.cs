using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.API.Services;
using Xunit;

namespace SafeHome.Tests
{
    public class SocialIntegrationDefaultMessageTests
    {
        /*[Fact]
        public async Task SocialIntegrationService_UsesDefaultMessage_WhenRequestMessageEmpty()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            var incidentId = await db.InsertIncidentAsync(buildingId, type: "Fire", status: "Open");

            var handler = new CapturingHandler();
            var client = new HttpClient(handler);
            var factory = new FakeHttpClientFactory(client);

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/api", Enabled = true }
            });

            var service = new SocialIntegrationService(db.ConnectionFactory, factory, options);

            await service.ShareIncidentAsync(incidentId, new SocialShareRequestDto { Network = "twitter", Message = "   " });

            Assert.NotNull(handler.LastBody);
            Assert.Contains("Atualização do incidente", handler.LastBody);
            Assert.Contains("Fire", handler.LastBody);
            Assert.Contains("HQ", handler.LastBody);
            Assert.Contains("Open", handler.LastBody);
        }*/

        [Fact]
        public async Task SocialIntegrationService_DoesNotSetAuthorizationHeader_WhenApiKeyMissing()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            var incidentId = await db.InsertIncidentAsync(buildingId, type: "Fire", status: "Open");

            var handler = new CapturingHandler();
            var client = new HttpClient(handler);
            var factory = new FakeHttpClientFactory(client);

            var options = Options.Create(new List<SocialNetworkOption>
            {
                new SocialNetworkOption { Name = "twitter", ApiUrl = "https://social.test/api", ApiKey = "", Enabled = true }
            });

            var service = new SocialIntegrationService(db.ConnectionFactory, factory, options);

            await service.ShareIncidentAsync(incidentId, new SocialShareRequestDto { Network = "twitter", Message = "ok" });

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
