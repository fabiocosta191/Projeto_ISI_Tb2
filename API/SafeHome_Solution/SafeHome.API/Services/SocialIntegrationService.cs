using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SafeHome.API.DTOs;
using SafeHome.Data;

namespace SafeHome.API.Services
{
    public class SocialIntegrationService : ISocialIntegrationService
    {
        private readonly AppDbContext _dbContext;

        public SocialIntegrationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SocialShareResultDto> ShareIncidentAsync(int incidentId, SocialShareRequestDto request)
        {
            var incident = await _dbContext.Incidents
                .Include(i => i.Building)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            if (incident == null)
            {
                throw new InvalidOperationException($"Incidente {incidentId} não encontrado.");
            }

            var buildingName = incident.Building?.Name ?? "Edifício desconhecido";
            var message = string.IsNullOrWhiteSpace(request.Message)
                ? $"Atualização do incidente '{incident.Type}' em {buildingName} (estado: {incident.Status})."
                : request.Message.Trim();

            var payload = $"[{request.Network}] {message}";
            var urlSafeBuilding = Uri.EscapeDataString(buildingName);
            var shareUrl = $"https://social.example/share?network={Uri.EscapeDataString(request.Network)}&building={urlSafeBuilding}&incident={incident.Id}";

            return new SocialShareResultDto
            {
                Network = request.Network,
                PayloadPreview = payload,
                ShareUrl = shareUrl,
                SentAtUtc = DateTime.UtcNow
            };
        }
    }
}
