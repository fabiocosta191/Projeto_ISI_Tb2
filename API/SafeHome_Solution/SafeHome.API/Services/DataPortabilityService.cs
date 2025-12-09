using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var query = _dbContext.SensorReadings
                .Include(r => r.Sensor)
                .ThenInclude(s => s.Building)
                .AsQueryable();

            if (sensorId.HasValue)
            {
                query = query.Where(r => r.SensorId == sensorId.Value);
            }

            var readings = await query
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,SensorId,SensorName,Building,Value,Timestamp");

            foreach (var reading in readings)
            {
                csv.AppendLine($"{reading.Id},{reading.SensorId},\"{reading.Sensor?.Name}\",\"{reading.Sensor?.Building?.Name}\",{reading.Value},{reading.Timestamp:O}");
            }

            return csv.ToString();
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
            var existingSensors = await _dbContext.Sensors
                .Where(s => sensorIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

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

                await _dbContext.SensorReadings.AddAsync(entity);
                summary.Imported++;
            }

            await _dbContext.SaveChangesAsync();
            return summary;
        }
    }
}
