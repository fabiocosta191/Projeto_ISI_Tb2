using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await Task.FromResult(_context.Users.ToList());
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await Task.FromResult(_context.Users.FirstOrDefault(u => u.Id == id));
        }

        public async Task<User> CreateAsync(string username, string password, string role)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                throw new InvalidOperationException("User already exists.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                Role = role
            };

            user.Id = (_context.Users.Any() ? _context.Users.Max(u => u.Id) : 0) + 1;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateRoleAsync(int id, string role)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;

            user.Role = role;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
