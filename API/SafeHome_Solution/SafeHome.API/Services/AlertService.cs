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
            return await Task.FromResult(_context.Alerts.ToList());
        }

        public async Task<Alert?> GetByIdAsync(int id)
        {
            return await Task.FromResult(_context.Alerts.FirstOrDefault(a => a.Id == id));
        }

        public async Task<Alert> CreateAsync(Alert alert)
        {
            var nextId = _context.Alerts.Any() ? _context.Alerts.Max(a => a.Id) + 1 : 1;
            alert.Id = alert.Id == 0 ? nextId : alert.Id;
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task<bool> UpdateAsync(int id, Alert alert)
        {
            var existing = _context.Alerts.FirstOrDefault(a => a.Id == id);
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
            var alert = _context.Alerts.FirstOrDefault(a => a.Id == id);
            if (alert == null) return false;

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
