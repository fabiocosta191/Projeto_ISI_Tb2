using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.Soap;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentsController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Incident>>> GetIncidents()
        {
            return await _incidentService.GetAllIncidents();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Incident>> GetIncident(int id)
        {
            var incident = await _incidentService.GetIncidentById(id);
            if (incident == null) return NotFound();

            return incident;
        }

        [HttpPost]
        public async Task<ActionResult<Incident>> PostIncident(Incident incident)
        {
            var created = await _incidentService.CreateIncident(incident);
            return CreatedAtAction(nameof(GetIncident), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutIncident(int id, Incident incident)
        {
            var updated = await _incidentService.UpdateIncident(id, incident);
            if (!updated) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncident(int id)
        {
            var deleted = await _incidentService.DeleteIncident(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
    }
}
