using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.DTOs;
using SafeHome.API.Services;

namespace SafeHome.API.Controllers
{
    [Route("api/data-portability")]
    [ApiController]
    [Authorize]
    public class DataPortabilityController : ControllerBase
    {
        private readonly IDataPortabilityService _dataPortabilityService;

        public DataPortabilityController(IDataPortabilityService dataPortabilityService)
        {
            _dataPortabilityService = dataPortabilityService;
        }

        [HttpGet("sensor-readings/export")]
        public async Task<IActionResult> ExportSensorReadings([FromQuery] int? sensorId = null)
        {
            var csv = await _dataPortabilityService.ExportSensorReadingsCsvAsync(sensorId);
            var fileName = sensorId.HasValue ? $"sensor-{sensorId.Value}-readings.csv" : "sensor-readings.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        [HttpPost("sensor-readings/import")]
        public async Task<ActionResult<ImportSummaryDto>> ImportSensorReadings([FromBody] IEnumerable<SensorReadingImportDto> readings)
        {
            var result = await _dataPortabilityService.ImportSensorReadingsAsync(readings);
            return Ok(result);
        }
    }
}
