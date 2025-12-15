using Microsoft.EntityFrameworkCore;
using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class SensorService : ISensorService
    {
        private readonly AppDbContext _context;

        public SensorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Sensor>> GetAllSensors()
        {
            return await _context.Sensors.ToListAsync();
        }

        public async Task<Sensor?> GetSensorById(int id)
        {
            return await _context.Sensors.FindAsync(id);
        }

        public async Task<Sensor> CreateSensor(Sensor sensor)
        {
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();
            return sensor;
        }

        public async Task<bool> UpdateSensor(int id, Sensor sensor)
        {
            var existingSensor = await _context.Sensors.FindAsync(id);
            if (existingSensor == null) return false;

            // Atualizar campos
            existingSensor.Name = sensor.Name;
            existingSensor.Type = sensor.Type;
            existingSensor.IsActive = sensor.IsActive;
            existingSensor.BuildingId = sensor.BuildingId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSensor(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null) return false;

            var sensorReadings = _context.SensorReadings.Where(r => r.SensorId == id);
            var sensorAlerts = _context.Alerts.Where(a => a.SensorId == id);

            _context.SensorReadings.RemoveRange(sensorReadings);
            _context.Alerts.RemoveRange(sensorAlerts);
            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}