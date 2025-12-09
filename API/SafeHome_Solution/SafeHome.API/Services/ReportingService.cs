using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
                TotalBuildings = await _dbContext.Buildings.CountAsync(),
                TotalSensors = await _dbContext.Sensors.CountAsync(),
                ActiveSensors = await _dbContext.Sensors.CountAsync(s => s.IsActive),
                OpenIncidents = await _dbContext.Incidents.CountAsync(i => i.Status != "Resolved"),
                ResolvedIncidents = await _dbContext.Incidents.CountAsync(i => i.Status == "Resolved"),
                OpenAlerts = await _dbContext.Alerts.CountAsync(a => !a.IsResolved),
                ResolvedAlerts = await _dbContext.Alerts.CountAsync(a => a.IsResolved),
                GeneratedAtUtc = DateTime.UtcNow
            };

            snapshot.InactiveSensors = snapshot.TotalSensors - snapshot.ActiveSensors;

            snapshot.Buildings = await _dbContext.Buildings
                .Select(b => new BuildingLoadDto
                {
                    BuildingId = b.Id,
                    BuildingName = b.Name,
                    SensorCount = b.Sensors.Count,
                    OpenIncidents = b.Incidents.Count(i => i.Status != "Resolved")
                })
                .OrderByDescending(b => b.OpenIncidents)
                .ThenByDescending(b => b.SensorCount)
                .ToListAsync();

            return snapshot;
        }

        public async Task<string> ExportAlertsCsvAsync()
        {
            var alerts = await _dbContext.Alerts
                .Include(a => a.Sensor)
                .ThenInclude(s => s.Building)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,Severity,IsResolved,SensorId,SensorName,Building,Message");

            foreach (var alert in alerts)
            {
                csv.AppendLine(
                    $"{alert.Id},{alert.Timestamp:O},{alert.Severity},{alert.IsResolved},{alert.SensorId},\"{alert.Sensor?.Name}\",\"{alert.Sensor?.Building?.Name}\",\"{alert.Message.Replace("\"", "''")}\"");
            }

            return csv.ToString();
        }

        public async Task<string> ExportIncidentsCsvAsync()
        {
            var incidents = await _dbContext.Incidents
                .Include(i => i.Building)
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Type,Severity,Status,Building,StartedAt,EndedAt,Description");

            foreach (var incident in incidents)
            {
                csv.AppendLine(
                    $"{incident.Id},{incident.Type},{incident.Severity},{incident.Status},\"{incident.Building?.Name}\",{incident.StartedAt:O},{incident.EndedAt:O},\"{incident.Description.Replace("\"", "''")}\"");
            }

            return csv.ToString();
        }
    }
}
