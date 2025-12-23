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
            var incident = await CreateIncident(new Incident
            {
                Type = type,
                Description = description,
                BuildingId = buildingId,
                StartedAt = DateTime.UtcNow,
                Status = "Reported",
                Severity = severity
            });

            return $"Incidente recebido pela Proteção Civil. ID: {incident.Id}";
        }

        public async Task<List<Incident>> GetUnresolvedIncidents()
        {
            return await Task.FromResult(_context.Incidents
                .Where(i => i.Status != "Resolved")
                .ToList());
        }

        public async Task<List<Incident>> GetAllIncidents()
        {
            return await Task.FromResult(_context.Incidents.ToList());
        }

        public async Task<Incident?> GetIncidentById(int id)
        {
            return await Task.FromResult(_context.Incidents.FirstOrDefault(i => i.Id == id));
        }

        public async Task<Incident> CreateIncident(Incident incident)
        {
            if (incident.StartedAt == default)
            {
                incident.StartedAt = DateTime.UtcNow;
            }

            var nextId = _context.Incidents.Any() ? _context.Incidents.Max(i => i.Id) + 1 : 1;
            incident.Id = incident.Id == 0 ? nextId : incident.Id;
            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        public async Task<bool> UpdateIncident(int id, Incident incident)
        {
            var existing = _context.Incidents.FirstOrDefault(i => i.Id == id);
            if (existing == null) return false;

            existing.Type = incident.Type;
            existing.Description = incident.Description;
            existing.BuildingId = incident.BuildingId;
            existing.StartedAt = incident.StartedAt;
            existing.EndedAt = incident.EndedAt;
            existing.Status = incident.Status;
            existing.Severity = incident.Severity;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteIncident(int id)
        {
            var incident = _context.Incidents.FirstOrDefault(i => i.Id == id);
            if (incident == null) return false;

            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
