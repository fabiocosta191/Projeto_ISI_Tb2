using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly AppDbContext _context;

        public BuildingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Building>> GetAllBuildings()
        {
            return await Task.FromResult(_context.Buildings.ToList());
        }

        public async Task<Building?> GetBuildingById(int id)
        {
            return await Task.FromResult(_context.Buildings.FirstOrDefault(b => b.Id == id));
        }

        public async Task<Building> CreateBuilding(Building building)
        {
            var nextId = _context.Buildings.Any() ? _context.Buildings.Max(b => b.Id) + 1 : 1;
            building.Id = building.Id == 0 ? nextId : building.Id;
            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();
            return building;
        }

        public async Task<bool> UpdateBuilding(int id, Building building)
        {
            if (id != building.Id) return false;

            var existing = _context.Buildings.FirstOrDefault(b => b.Id == id);
            if (existing == null) return false;

            existing.Name = building.Name;
            existing.Address = building.Address;
            existing.Latitude = building.Latitude;
            existing.Longitude = building.Longitude;
            existing.RiskType = building.RiskType;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBuilding(int id)
        {
            var building = _context.Buildings.FirstOrDefault(b => b.Id == id);
            if (building == null) return false;

            var incidents = _context.Incidents.Where(i => i.BuildingId == id).ToList();
            foreach (var incident in incidents)
            {
                _context.Incidents.Remove(incident);
            }

            var sensors = _context.Sensors.Where(s => s.BuildingId == id).ToList();
            if (sensors.Any())
            {
                var sensorIds = sensors.Select(s => s.Id).ToList();
                var readingsToRemove = _context.SensorReadings.Where(r => sensorIds.Contains(r.SensorId)).ToList();
                var alertsToRemove = _context.Alerts.Where(a => sensorIds.Contains(a.SensorId)).ToList();

                foreach (var reading in readingsToRemove)
                {
                    _context.SensorReadings.Remove(reading);
                }

                foreach (var alert in alertsToRemove)
                {
                    _context.Alerts.Remove(alert);
                }

                foreach (var sensor in sensors)
                {
                    _context.Sensors.Remove(sensor);
                }
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
