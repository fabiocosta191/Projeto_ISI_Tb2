using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class SensorReadingService : ISensorReadingService
    {
        private readonly AppDbContext _context;

        public SensorReadingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SensorReading>> GetAllAsync()
        {
            return await Task.FromResult(_context.SensorReadings.ToList());
        }

        public async Task<SensorReading?> GetByIdAsync(int id)
        {
            return await Task.FromResult(_context.SensorReadings.FirstOrDefault(r => r.Id == id));
        }

        public async Task<SensorReading> CreateAsync(SensorReading reading)
        {
            var nextId = _context.SensorReadings.Any() ? _context.SensorReadings.Max(r => r.Id) + 1 : 1;
            reading.Id = reading.Id == 0 ? nextId : reading.Id;
            _context.SensorReadings.Add(reading);
            await _context.SaveChangesAsync();
            return reading;
        }

        public async Task<bool> UpdateAsync(int id, SensorReading reading)
        {
            var existing = _context.SensorReadings.FirstOrDefault(r => r.Id == id);
            if (existing == null) return false;

            existing.Value = reading.Value;
            existing.Timestamp = reading.Timestamp;
            existing.SensorId = reading.SensorId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reading = _context.SensorReadings.FirstOrDefault(r => r.Id == id);
            if (reading == null) return false;

            _context.SensorReadings.Remove(reading);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
