using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public interface ISensorReadingService
    {
        Task<List<SensorReading>> GetAllAsync();
        Task<SensorReading?> GetByIdAsync(int id);
        Task<SensorReading> CreateAsync(SensorReading reading);
        Task<bool> UpdateAsync(int id, SensorReading reading);
        Task<bool> DeleteAsync(int id);
    }
}
