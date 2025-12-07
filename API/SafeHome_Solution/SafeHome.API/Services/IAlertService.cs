using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public interface IAlertService
    {
        Task<List<Alert>> GetAllAsync();
        Task<Alert?> GetByIdAsync(int id);
        Task<Alert> CreateAsync(Alert alert);
        Task<bool> UpdateAsync(int id, Alert alert);
        Task<bool> DeleteAsync(int id);
    }
}
