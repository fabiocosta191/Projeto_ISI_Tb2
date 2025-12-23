using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.DTOs;
using SafeHome.API.Services;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _userService.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(CreateUserDto dto)
        {
            try
            {
                var created = await _userService.CreateAsync(dto.Username, dto.Password, dto.Role);
                return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserDto dto)
        {
            var updatedRole = await _userService.UpdateRoleAsync(id, dto.Role);
            if (!updatedRole) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var passwordUpdated = await _userService.ResetPasswordAsync(id, dto.NewPassword);
                if (!passwordUpdated) return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
    }
}
