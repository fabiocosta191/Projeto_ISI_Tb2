using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.Services;

namespace SafeHome.API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportsController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var snapshot = await _reportingService.GetDashboardAsync();
            return Ok(snapshot);
        }

        [HttpGet("export/alerts")]
        public async Task<IActionResult> ExportAlerts()
        {
            var csv = await _reportingService.ExportAlertsCsvAsync();
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "alerts.csv");
        }

        [HttpGet("export/incidents")]
        public async Task<IActionResult> ExportIncidents()
        {
            var csv = await _reportingService.ExportIncidentsCsvAsync();
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "incidents.csv");
        }
    }
}
