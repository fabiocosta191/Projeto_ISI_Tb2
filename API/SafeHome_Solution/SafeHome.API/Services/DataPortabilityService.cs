using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SafeHome.API.DTOs;
using SafeHome.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class DataPortabilityService : IDataPortabilityService
    {
        private readonly AppDbContext _dbContext;

        public DataPortabilityService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> ExportSensorReadingsCsvAsync(int? sensorId = null)
        {
            var readings = _dbContext.SensorReadings
                .Where(r => !sensorId.HasValue || r.SensorId == sensorId.Value)
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Id,SensorId,SensorName,Building,Value,Timestamp");

            foreach (var reading in readings)
            {
                var sensor = _dbContext.Sensors.FirstOrDefault(s => s.Id == reading.SensorId);
                var building = sensor != null ? _dbContext.Buildings.FirstOrDefault(b => b.Id == sensor.BuildingId) : null;

                csv.AppendLine($"{reading.Id},{reading.SensorId},\"{sensor?.Name}\",\"{building?.Name}\",{reading.Value},{reading.Timestamp:O}");
            }

            return await Task.FromResult(csv.ToString());
        }

        public async Task<ImportSummaryDto> ImportSensorReadingsAsync(IEnumerable<SensorReadingImportDto> readings)
        {
            var summary = new ImportSummaryDto();
            var readingsList = readings.ToList();

            if (!readingsList.Any())
            {
                summary.Notes.Add("Nenhuma leitura fornecida.");
                return summary;
            }

            var sensorIds = readingsList.Select(r => r.SensorId).Distinct().ToList();
            var existingSensors = _dbContext.Sensors
                .Where(s => sensorIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToList();

            foreach (var readingDto in readingsList)
            {
                if (!existingSensors.Contains(readingDto.SensorId))
                {
                    summary.Skipped++;
                    summary.Notes.Add($"Sensor {readingDto.SensorId} inexistente. Leitura ignorada.");
                    continue;
                }

                var entity = new SensorReading
                {
                    SensorId = readingDto.SensorId,
                    Value = readingDto.Value ?? 0,
                    Timestamp = readingDto.Timestamp ?? DateTime.UtcNow
                };

                _dbContext.SensorReadings.Add(entity);
                summary.Imported++;
            }

            await _dbContext.SaveChangesAsync();
            return summary;
        }
    }
}
