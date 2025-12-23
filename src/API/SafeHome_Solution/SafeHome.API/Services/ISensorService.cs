using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public interface ISensorService
    {
        Task<List<Sensor>> GetAllSensors();
        Task<Sensor?> GetSensorById(int id);
        Task<Sensor> CreateSensor(Sensor sensor);
        Task<bool> UpdateSensor(int id, Sensor sensor);
        Task<bool> DeleteSensor(int id);
    }
}