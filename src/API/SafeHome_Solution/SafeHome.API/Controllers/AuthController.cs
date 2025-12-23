using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SafeHome.API.Data;
using SafeHome.API.DTOs;
using SafeHome.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static readonly HashSet<string> AllowedRoles = ["Admin", "User"];

        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;

        public AuthController(IDbConnectionFactory connectionFactory, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<object>> Register(RegisterDto request)
        {
            if (!AllowedRoles.Contains(request.Role))
            {
                return BadRequest("Invalid role. Allowed roles: Admin, User.");
            }

            const string existsSql = "SELECT 1 FROM Users WHERE Username = @Username";
            const string insertSql = "INSERT INTO Users (Username, PasswordHash, Role) OUTPUT INSERTED.Id VALUES (@Username, @PasswordHash, @Role)";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var existsCommand = new SqlCommand(existsSql, connection))
            {
                existsCommand.Parameters.AddWithValue("@Username", request.Username);
                var exists = await existsCommand.ExecuteScalarAsync();
                if (exists != null)
                {
                    return BadRequest("User already exists.");
                }
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                Role = request.Role
            };

            await using (var insertCommand = new SqlCommand(insertSql, connection))
            {
                insertCommand.Parameters.AddWithValue("@Username", user.Username);
                insertCommand.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                insertCommand.Parameters.AddWithValue("@Role", user.Role);
                user.Id = (int)await insertCommand.ExecuteScalarAsync();
            }

            return CreatedAtAction(nameof(Register), new { user.Id }, new { user.Id, user.Username, user.Role });
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            var user = await GetUserByUsernameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("User not found or wrong password.");
            }

            string token = CreateToken(user);

            return Ok(token);
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = await GetUserByUsernameAsync(username);
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.Username, user.Role });
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = await GetUserByUsernameAsync(username);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("A password atual n\u01DCo est\u01ED correta.");
            }

            const string sql = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id";
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
            command.Parameters.AddWithValue("@Id", user.Id);
            await command.ExecuteNonQueryAsync();

            return Ok(new { Message = "Password alterada com sucesso." });
        }

        private async Task<User?> GetUserByUsernameAsync(string username)
        {
            const string sql = "SELECT Id, Username, PasswordHash, Role FROM Users WHERE Username = @Username";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role"))
            };
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
