using Microsoft.EntityFrameworkCore;
using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Soap
{
    public class IncidentService : IIncidentService
    {
        private readonly AppDbContext _context;

        // Injetar a Base de Dados
        public IncidentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> ReportIncident(string type, string description, int buildingId, string severity)
        {
            var incident = new Incident
            {
                Type = type,
                Description = description,
                BuildingId = buildingId,
                StartedAt = DateTime.UtcNow,
                Status = "Reported",
                Severity = severity
            };

            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            return $"Incidente recebido pela Proteção Civil. ID: {incident.Id}";
        }

        public async Task<List<Incident>> GetUnresolvedIncidents()
        {
            return await _context.Incidents
                .Where(i => i.Status != "Resolved")
                .Include(i => i.Building) // Traz os dados do edifício
                .ToListAsync();
        }
    }
}