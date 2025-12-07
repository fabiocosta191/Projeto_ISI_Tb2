using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.Services;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alert>>> GetAlerts()
        {
            return await _alertService.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Alert>> GetAlert(int id)
        {
            var alert = await _alertService.GetByIdAsync(id);
            if (alert == null) return NotFound();

            return alert;
        }

        [HttpPost]
        public async Task<ActionResult<Alert>> PostAlert(Alert alert)
        {
            var created = await _alertService.CreateAsync(alert);
            return CreatedAtAction(nameof(GetAlert), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlert(int id, Alert alert)
        {
            var updated = await _alertService.UpdateAsync(id, alert);
            if (!updated) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var deleted = await _alertService.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
    }
}
