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
            return await Task.FromResult(_context.Sensors.ToList());
        }

        public async Task<Sensor?> GetSensorById(int id)
        {
            return await Task.FromResult(_context.Sensors.FirstOrDefault(s => s.Id == id));
        }

        public async Task<Sensor> CreateSensor(Sensor sensor)
        {
            var nextId = _context.Sensors.Any() ? _context.Sensors.Max(s => s.Id) + 1 : 1;
            sensor.Id = sensor.Id == 0 ? nextId : sensor.Id;
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();
            return sensor;
        }

        public async Task<bool> UpdateSensor(int id, Sensor sensor)
        {
            var existingSensor = _context.Sensors.FirstOrDefault(s => s.Id == id);
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
            var sensor = _context.Sensors.FirstOrDefault(s => s.Id == id);
            if (sensor == null) return false;

            var sensorReadings = _context.SensorReadings.Where(r => r.SensorId == id).ToList();
            var sensorAlerts = _context.Alerts.Where(a => a.SensorId == id).ToList();

            foreach (var reading in sensorReadings)
            {
                _context.SensorReadings.Remove(reading);
            }

            foreach (var alert in sensorAlerts)
            {
                _context.Alerts.Remove(alert);
            }

            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
