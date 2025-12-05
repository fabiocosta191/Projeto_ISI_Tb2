using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.DTOs;
using SafeHome.API.Services;
using SafeHome.Data.Models;

namespace SafeHome.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public SensorsController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        /// <summary>
        /// Obtém a lista de todos os sensores.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetSensors()
        {
            return await _sensorService.GetAllSensors();
        }

        /// <summary>
        /// Obtém um sensor pelo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Sensor), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Sensor>> GetSensor(int id)
        {
            var sensor = await _sensorService.GetSensorById(id);

            if (sensor == null)
            {
                return NotFound();
            }

            return sensor;
        }

        /// <summary>
        /// Cria um novo sensor.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Sensor), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Sensor>> PostSensor(CreateSensorDto sensorDto)
        {
            var sensor = new Sensor
            {
                Name = sensorDto.Name,
                Type = sensorDto.Type,
                // Se vier null, assume 0 (mas vamos tratar o erro se 0 não existir)
                BuildingId = sensorDto.BuildingId ?? 0,
                IsActive = true
            };

            try
            {
                var createdSensor = await _sensorService.CreateSensor(sensor);
                return CreatedAtAction("GetSensor", new { id = createdSensor.Id }, createdSensor);
            }
            catch (Exception ex)
            {
                // Verifica se o erro foi causado por violação de Chave Estrangeira (FK)
                // Isto acontece se tentares inserir um BuildingId que não existe na tabela Buildings
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY"))
                {
                    return BadRequest($"Erro: O Edifício com ID {sensor.BuildingId} não existe. Cria o edifício primeiro.");
                }

                // Se for outro erro qualquer, lança-o para ser tratado pelo servidor
                throw;
            }
        }

        /// <summary>
        /// Atualiza um sensor existente.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PutSensor(int id, UpdateSensorDto sensorDto)
        {
            // Mapear DTO -> Entidade
            var sensor = new Sensor
            {
                Id = id,
                Name = sensorDto.Name,
                Type = sensorDto.Type,
                IsActive = sensorDto.IsActive,
                // CORREÇÃO AQUI: Se for null, assume 0
                BuildingId = sensorDto.BuildingId ?? 0
            };

            var result = await _sensorService.UpdateSensor(id, sensor);

            if (!result)
            {
                return BadRequest("Erro ao atualizar: ID não encontrado ou inválido.");
            }

            return NoContent();
        }

        /// <summary>
        /// Remove um sensor.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteSensor(int id)
        {
            var result = await _sensorService.DeleteSensor(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}