using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeHome.API.DTOs;
using SafeHome.Data;
using SafeHome.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(LoginDto request)
        {
            // Verifica se o user já existe
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("User already exists.");
            }

            // Cria o utilizador (NOTA: Em produção, usa-se BCrypt para a password!)
            var user = new User
            {
                Username = request.Username,
                PasswordHash = request.Password, // Simplificado para aula
                Role = "Admin"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            // 1. Verificar na Base de Dados
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                return BadRequest("User not found or wrong password.");
            }

            // 2. Criar o Token (O "Crachá")
            string token = CreateToken(user);

            return Ok(token);
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