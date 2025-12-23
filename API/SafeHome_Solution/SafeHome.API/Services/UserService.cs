using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class UserService : IUserService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<User>> GetAllAsync()
        {
            const string sql = "SELECT Id, Username, PasswordHash, Role FROM Users";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var users = new List<User>();

            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = "SELECT Id, Username, PasswordHash, Role FROM Users WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }

        public async Task<User> CreateAsync(string username, string password, string role)
        {
            const string existsSql = "SELECT 1 FROM Users WHERE Username = @Username";
            const string insertSql = "INSERT INTO Users (Username, PasswordHash, Role) OUTPUT INSERTED.Id VALUES (@Username, @PasswordHash, @Role)";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var existsCommand = new SqlCommand(existsSql, connection))
            {
                existsCommand.Parameters.AddWithValue("@Username", username);
                var exists = await existsCommand.ExecuteScalarAsync();
                if (exists != null)
                {
                    throw new InvalidOperationException("User already exists.");
                }
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            await using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@Username", username);
            insertCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            insertCommand.Parameters.AddWithValue("@Role", role);
            var newId = (int)await insertCommand.ExecuteScalarAsync();

            return new User
            {
                Id = newId,
                Username = username,
                PasswordHash = hashedPassword,
                Role = role
            };
        }

        public async Task<bool> UpdateRoleAsync(int id, string role)
        {
            const string sql = "UPDATE Users SET Role = @Role WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Role", role);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            const string sql = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Users WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role"))
            };
        }
    }
}
