using Microsoft.EntityFrameworkCore;
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
            return await _context.Buildings.ToListAsync();
        }

        public async Task<Building?> GetBuildingById(int id)
        {
            return await _context.Buildings.FindAsync(id);
        }

        public async Task<Building> CreateBuilding(Building building)
        {
            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();
            return building;
        }

        public async Task<bool> UpdateBuilding(int id, Building building)
        {
            if (id != building.Id) return false;

            _context.Entry(building).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BuildingExists(id)) return false;
                else throw;
            }
        }

        public async Task<bool> DeleteBuilding(int id)
        {
            var building = await _context.Buildings
                .Include(b => b.Sensors)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (building == null) return false;

            var incidents = _context.Incidents.Where(i => i.BuildingId == id);
            _context.Incidents.RemoveRange(incidents);

            if (building.Sensors != null && building.Sensors.Any())
            {
                var sensorIds = building.Sensors.Select(s => s.Id).ToList();
                var readingsToRemove = _context.SensorReadings.Where(r => sensorIds.Contains(r.SensorId));
                var alertsToRemove = _context.Alerts.Where(a => sensorIds.Contains(a.SensorId));

                _context.SensorReadings.RemoveRange(readingsToRemove);
                _context.Alerts.RemoveRange(alertsToRemove);
                _context.Sensors.RemoveRange(building.Sensors);
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> BuildingExists(int id)
        {
            return await _context.Buildings.AnyAsync(e => e.Id == id);
        }
    }
}