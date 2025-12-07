using Microsoft.EntityFrameworkCore;
using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class AlertService : IAlertService
    {
        private readonly AppDbContext _context;

        public AlertService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Alert>> GetAllAsync()
        {
            return await _context.Alerts
                .Include(a => a.Sensor)
                .ToListAsync();
        }

        public async Task<Alert?> GetByIdAsync(int id)
        {
            return await _context.Alerts
                .Include(a => a.Sensor)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Alert> CreateAsync(Alert alert)
        {
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task<bool> UpdateAsync(int id, Alert alert)
        {
            var existing = await _context.Alerts.FindAsync(id);
            if (existing == null) return false;

            existing.Message = alert.Message;
            existing.Severity = alert.Severity;
            existing.IsResolved = alert.IsResolved;
            existing.Timestamp = alert.Timestamp;
            existing.SensorId = alert.SensorId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return false;

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
