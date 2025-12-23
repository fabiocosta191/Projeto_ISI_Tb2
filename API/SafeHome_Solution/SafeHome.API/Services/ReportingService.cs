using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SafeHome.API.DTOs;
using SafeHome.Data;

namespace SafeHome.API.Services
{
    public class ReportingService : IReportingService
    {
        private readonly AppDbContext _dbContext;

        public ReportingService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardSnapshotDto> GetDashboardAsync()
        {
            var snapshot = new DashboardSnapshotDto
            {
                TotalBuildings = _dbContext.Buildings.Count,
                TotalSensors = _dbContext.Sensors.Count,
                ActiveSensors = _dbContext.Sensors.Count(s => s.IsActive),
                OpenIncidents = _dbContext.Incidents.Count(i => i.Status != "Resolved"),
                ResolvedIncidents = _dbContext.Incidents.Count(i => i.Status == "Resolved"),
                OpenAlerts = _dbContext.Alerts.Count(a => !a.IsResolved),
                ResolvedAlerts = _dbContext.Alerts.Count(a => a.IsResolved),
                GeneratedAtUtc = DateTime.UtcNow
            };

            snapshot.InactiveSensors = snapshot.TotalSensors - snapshot.ActiveSensors;

            snapshot.Buildings = _dbContext.Buildings
                .Select(b => new BuildingLoadDto
                {
                    BuildingId = b.Id,
                    BuildingName = b.Name,
                    SensorCount = _dbContext.Sensors.Count(s => s.BuildingId == b.Id),
                    OpenIncidents = _dbContext.Incidents.Count(i => i.BuildingId == b.Id && i.Status != "Resolved")
                })
                .OrderByDescending(b => b.OpenIncidents)
                .ThenByDescending(b => b.SensorCount)
                .ToList();

            return await Task.FromResult(snapshot);
        }

        public async Task<string> ExportAlertsCsvAsync()
        {
            var alerts = _dbContext.Alerts
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,Severity,IsResolved,SensorId,SensorName,Building,Message");

            foreach (var alert in alerts)
            {
                var sensor = _dbContext.Sensors.FirstOrDefault(s => s.Id == alert.SensorId);
                var building = sensor != null ? _dbContext.Buildings.FirstOrDefault(b => b.Id == sensor.BuildingId) : null;

                csv.AppendLine(
                    $"{alert.Id},{alert.Timestamp:O},{alert.Severity},{alert.IsResolved},{alert.SensorId},\"{sensor?.Name}\",\"{building?.Name}\",\"{alert.Message.Replace("\"", "''")}\"");
            }

            return await Task.FromResult(csv.ToString());
        }

        public async Task<string> ExportIncidentsCsvAsync()
        {
            var incidents = _dbContext.Incidents
                .OrderByDescending(i => i.StartedAt)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Type,Severity,Status,Building,StartedAt,EndedAt,Description");

            foreach (var incident in incidents)
            {
                var building = _dbContext.Buildings.FirstOrDefault(b => b.Id == incident.BuildingId);

                csv.AppendLine(
                    $"{incident.Id},{incident.Type},{incident.Severity},{incident.Status},\"{building?.Name}\",{incident.StartedAt:O},{incident.EndedAt:O},\"{incident.Description?.Replace("\"", "''") ?? ""}\"");
            }

            return await Task.FromResult(csv.ToString());
        }
    }
}
