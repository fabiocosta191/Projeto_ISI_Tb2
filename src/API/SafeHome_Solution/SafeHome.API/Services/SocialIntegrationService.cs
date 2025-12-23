using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SafeHome.API.Data;
using SafeHome.API.DTOs;
using SafeHome.API.Options;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class SocialIntegrationService : ISocialIntegrationService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<List<SocialNetworkOption>> _networkOptions;

        public SocialIntegrationService(
            IDbConnectionFactory connectionFactory,
            IHttpClientFactory httpClientFactory,
            IOptions<List<SocialNetworkOption>> networkOptions)
        {
            _connectionFactory = connectionFactory;
            _httpClientFactory = httpClientFactory;
            _networkOptions = networkOptions;
        }

        public async Task<SocialShareResultDto> ShareIncidentAsync(int incidentId, SocialShareRequestDto request)
        {
            const string sql = @"SELECT i.Id, i.Type, i.Status, i.BuildingId,
                                        b.Id AS Building_Id, b.Name AS BuildingName
                                 FROM Incidents i
                                 LEFT JOIN Buildings b ON i.BuildingId = b.Id
                                 WHERE i.Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", incidentId);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException($"Incidente {incidentId} n\u00C7\u0153o encontrado.");
            }

            var buildingNameOrdinal = reader.GetOrdinal("BuildingName");
            Building? building = null;
            if (!reader.IsDBNull(buildingNameOrdinal))
            {
                building = new Building
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Building_Id")),
                    Name = reader.GetString(buildingNameOrdinal)
                };
            }

            var incident = new Incident
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId")),
                Building = building
            };

            var selectedNetwork = _networkOptions.Value
                .FirstOrDefault(n => n.Enabled && n.Name.Equals(request.Network, StringComparison.OrdinalIgnoreCase));

            if (selectedNetwork == null)
            {
                throw new InvalidOperationException($"Rede social '{request.Network}' n\u00C7\u0153o configurada ou desativada.");
            }

            var buildingName = incident.Building?.Name ?? "Edif\u00C7\u00F0cio desconhecido";
            var message = string.IsNullOrWhiteSpace(request.Message)
                ? $"Atualiza\u00C7\u00F5\u00C7\u0153o do incidente '{incident.Type}' em {buildingName} (estado: {incident.Status})."
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
