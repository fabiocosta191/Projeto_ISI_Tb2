using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(string username, string password, string role);
        Task<bool> UpdateRoleAsync(int id, string role);
        Task<bool> ResetPasswordAsync(int id, string newPassword);
        Task<bool> DeleteAsync(int id);
    }
}
