using Microsoft.EntityFrameworkCore;
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
            return await _context.SensorReadings
                .Include(r => r.Sensor)
                .ToListAsync();
        }

        public async Task<SensorReading?> GetByIdAsync(int id)
        {
            return await _context.SensorReadings
                .Include(r => r.Sensor)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<SensorReading> CreateAsync(SensorReading reading)
        {
            _context.SensorReadings.Add(reading);
            await _context.SaveChangesAsync();
            return reading;
        }

        public async Task<bool> UpdateAsync(int id, SensorReading reading)
        {
            var existing = await _context.SensorReadings.FindAsync(id);
            if (existing == null) return false;

            existing.Value = reading.Value;
            existing.Timestamp = reading.Timestamp;
            existing.SensorId = reading.SensorId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reading = await _context.SensorReadings.FindAsync(id);
            if (reading == null) return false;

            _context.SensorReadings.Remove(reading);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
