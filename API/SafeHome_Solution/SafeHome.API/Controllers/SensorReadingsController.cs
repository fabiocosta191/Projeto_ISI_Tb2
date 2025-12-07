using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.Services;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SensorReadingsController : ControllerBase
    {
        private readonly ISensorReadingService _sensorReadingService;

        public SensorReadingsController(ISensorReadingService sensorReadingService)
        {
            _sensorReadingService = sensorReadingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SensorReading>>> GetReadings()
        {
            return await _sensorReadingService.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SensorReading>> GetReading(int id)
        {
            var reading = await _sensorReadingService.GetByIdAsync(id);
            if (reading == null) return NotFound();

            return reading;
        }

        [HttpPost]
        public async Task<ActionResult<SensorReading>> PostReading(SensorReading reading)
        {
            var created = await _sensorReadingService.CreateAsync(reading);
            return CreatedAtAction(nameof(GetReading), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReading(int id, SensorReading reading)
        {
            var updated = await _sensorReadingService.UpdateAsync(id, reading);
            if (!updated) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReading(int id)
        {
            var deleted = await _sensorReadingService.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
    }
}
