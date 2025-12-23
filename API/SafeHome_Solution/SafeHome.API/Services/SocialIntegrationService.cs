using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.Data;

namespace SafeHome.API.Services
{
    public class SocialIntegrationService : ISocialIntegrationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<List<SocialNetworkOption>> _networkOptions;

        public SocialIntegrationService(
            AppDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IOptions<List<SocialNetworkOption>> networkOptions)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _networkOptions = networkOptions;
        }

        public async Task<SocialShareResultDto> ShareIncidentAsync(int incidentId, SocialShareRequestDto request)
        {
            var incident = _dbContext.Incidents.FirstOrDefault(i => i.Id == incidentId);
            var building = incident != null ? _dbContext.Buildings.FirstOrDefault(b => b.Id == incident.BuildingId) : null;

            if (incident == null)
            {
                throw new InvalidOperationException($"Incidente {incidentId} não encontrado.");
            }

            var selectedNetwork = _networkOptions.Value
                .FirstOrDefault(n => n.Enabled && n.Name.Equals(request.Network, StringComparison.OrdinalIgnoreCase));

            if (selectedNetwork == null)
            {
                throw new InvalidOperationException($"Rede social '{request.Network}' não configurada ou desativada.");
            }

            var buildingName = building?.Name ?? "Edifício desconhecido";
            var message = string.IsNullOrWhiteSpace(request.Message)
                ? $"Atualização do incidente '{incident.Type}' em {buildingName} (estado: {incident.Status})."
                : request.Message.Trim();

            var payload = new
            {
                incidentId = incident.Id,
                building = buildingName,
                status = incident.Status,
                message,
                network = selectedNetwork.Name
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpClient = _httpClientFactory.CreateClient("social-sharing");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, selectedNetwork.ApiUrl)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(selectedNetwork.ApiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", selectedNetwork.ApiKey);
            }

            var httpResponse = await httpClient.SendAsync(httpRequest);
            var responsePreview = (await httpResponse.Content.ReadAsStringAsync()) ?? string.Empty;

            return new SocialShareResultDto
            {
                Network = selectedNetwork.Name,
                PayloadPreview = payloadJson,
                ShareUrl = selectedNetwork.ApiUrl,
                SentAtUtc = DateTime.UtcNow,
                ExternalStatusCode = (int)httpResponse.StatusCode,
                ExternalResponsePreview = responsePreview.Length > 200
                    ? responsePreview[..200]
                    : responsePreview
            };
        }
    }
}
