using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHome.API.Services;
using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protegido!
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWeatherService _weatherService;

        // Injetamos a BD e o Serviço de Meteorologia
        public BuildingsController(AppDbContext context, IWeatherService weatherService)
        {
            _context = context;
            _weatherService = weatherService;
        }

        // POST: api/Buildings
        [HttpPost]
        public async Task<ActionResult<Building>> PostBuilding(Building building)
        {
            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, building);
        }

        // GET: api/Buildings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetBuilding(int id)
        {
            var building = await _context.Buildings.FindAsync(id);

            if (building == null) return NotFound();

            // Vamos buscar a meteorologia para este local
            var weather = await _weatherService.GetCurrentWeather(building.Latitude, building.Longitude);

            // Devolvemos o Edifício + O tempo atual
            return new
            {
                DadosEdificio = building,
                MeteorologiaAtual = weather
            };
        }
    }
}