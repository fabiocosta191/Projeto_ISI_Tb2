using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.Services;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BuildingsController : ControllerBase
    {
        private readonly IBuildingService _buildingService;
        private readonly IWeatherService _weatherService;

        public BuildingsController(IBuildingService buildingService, IWeatherService weatherService)
        {
            _buildingService = buildingService;
            _weatherService = weatherService;
        }

        /// <summary>
        /// Lista todos os edifícios registados.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Building>>> GetBuildings()
        {
            return await _buildingService.GetAllBuildings();
        }

        /// <summary>
        /// Obtém detalhes de um edifício e a meteorologia atual do local.
        /// </summary>
        /// <param name="id">ID do edifício</param>
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> GetBuilding(int id)
        {
            var building = await _buildingService.GetBuildingById(id);

            if (building == null) return NotFound();

            // Integração Externa: Buscar Meteorologia
            var weather = await _weatherService.GetCurrentWeather(building.Latitude, building.Longitude);

            return new
            {
                DadosEdificio = building,
                MeteorologiaAtual = weather
            };
        }

        /// <summary>
        /// Regista um novo edifício.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<ActionResult<Building>> PostBuilding(Building building)
        {
            var created = await _buildingService.CreateBuilding(building);
            return CreatedAtAction(nameof(GetBuilding), new { id = created.Id }, created);
        }

        /// <summary>
        /// Atualiza os dados de um edifício.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PutBuilding(int id, Building building)
        {
            if (id != building.Id) return BadRequest("ID do URL não corresponde ao ID do corpo.");

            var updated = await _buildingService.UpdateBuilding(id, building);

            if (!updated) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Remove um edifício.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            var deleted = await _buildingService.DeleteBuilding(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
    }
}