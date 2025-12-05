using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public interface IBuildingService
    {
        Task<List<Building>> GetAllBuildings();
        Task<Building?> GetBuildingById(int id);
        Task<Building> CreateBuilding(Building building);
        Task<bool> UpdateBuilding(int id, Building building);
        Task<bool> DeleteBuilding(int id);
    }
}
