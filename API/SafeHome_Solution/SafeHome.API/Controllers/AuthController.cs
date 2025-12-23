using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeHome.API.DTOs;
using SafeHome.Data;
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

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
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

            if (_context.Users.Any(u => u.Username == request.Username))
            {
                return BadRequest("User already exists.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                Role = request.Role
            };

            user.Id = (_context.Users.LastOrDefault()?.Id ?? 0) + 1;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { user.Id }, new { user.Id, user.Username, user.Role });
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Username == request.Username);

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

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.Username, user.Role });
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("A password atual não está correta.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password alterada com sucesso." });
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
