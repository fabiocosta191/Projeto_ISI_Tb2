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
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return false;

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