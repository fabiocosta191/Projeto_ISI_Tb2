using SafeHome.API.DTOs;
using SafeHome.API.Services;
using SafeHome.Data;
using SafeHome.Data.Models;
using Xunit;

namespace SafeHome.Tests
{
    public class ReportingAndPortabilityEdgeTests
    {
        private static AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        [Fact]
        public async Task ReportingService_ComputesInactiveSensorsCorrectly()
        {
            using var context = CreateContext();

            context.Buildings.Add(new Building { Id = 1, Name = "A" });
            context.Sensors.AddRange(
                new Sensor { Id = 1, Name = "S1", BuildingId = 1, IsActive = true },
                new Sensor { Id = 2, Name = "S2", BuildingId = 1, IsActive = false },
                new Sensor { Id = 3, Name = "S3", BuildingId = 1, IsActive = false }
            );

            await context.SaveChangesAsync();

            var service = new ReportingService(context);
            var snapshot = await service.GetDashboardAsync();

            Assert.Equal(3, snapshot.TotalSensors);
            Assert.Equal(1, snapshot.ActiveSensors);
            Assert.Equal(2, snapshot.InactiveSensors);
        }

        [Fact]
        public async Task ReportingService_BuildingsOrderedByOpenIncidentsThenSensorCount()
        {
            using var context = CreateContext();

            var a = new Building { Id = 1, Name = "A" };
            var b = new Building { Id = 2, Name = "B" };
            context.Buildings.AddRange(a, b);

            // A: 2 incidentes abertos, 1 sensor
            context.Sensors.Add(new Sensor { Id = 10, Name = "A1", BuildingId = 1, IsActive = true });
            context.Incidents.AddRange(
                new Incident { Id = 100, BuildingId = 1, Type = "Fire", Status = "Open", StartedAt = DateTime.UtcNow },
                new Incident { Id = 101, BuildingId = 1, Type = "Leak", Status = "Open", StartedAt = DateTime.UtcNow }
            );

            // B: 1 incidente aberto, 5 sensores
            context.Sensors.AddRange(
                new Sensor { Id = 20, Name = "B1", BuildingId = 2, IsActive = true },
                new Sensor { Id = 21, Name = "B2", BuildingId = 2, IsActive = true },
                new Sensor { Id = 22, Name = "B3", BuildingId = 2, IsActive = true },
                new Sensor { Id = 23, Name = "B4", BuildingId = 2, IsActive = true },
                new Sensor { Id = 24, Name = "B5", BuildingId = 2, IsActive = true }
            );
            context.Incidents.Add(new Incident { Id = 200, BuildingId = 2, Type = "Power", Status = "Open", StartedAt = DateTime.UtcNow });

            await context.SaveChangesAsync();

            var service = new ReportingService(context);
            var snapshot = await service.GetDashboardAsync();

            Assert.NotEmpty(snapshot.Buildings);
            Assert.Equal(1, snapshot.Buildings[0].BuildingId); // A primeiro (mais incidentes abertos)
        }

        [Fact]
        public async Task ReportingService_ExportAlertsCsv_ReturnsHeaderOnly_WhenNoAlerts()
        {
            using var context = CreateContext();
            var service = new ReportingService(context);

            var csv = await service.ExportAlertsCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Single(lines);
            Assert.StartsWith("Id,Timestamp,Severity", lines[0]);
        }

        [Fact]
        public async Task ReportingService_ExportIncidentsCsv_EscapesQuotesInDescription()
        {
            using var context = CreateContext();
            context.Buildings.Add(new Building { Id = 1, Name = "HQ" });
            context.Incidents.Add(new Incident
            {
                Id = 1,
                BuildingId = 1,
                Type = "Fire",
                Severity = "High",
                Status = "Open",
                StartedAt = DateTime.UtcNow,
                Description = "Trigger \"A\" detected"
            });
            await context.SaveChangesAsync();

            var service = new ReportingService(context);
            var csv = await service.ExportIncidentsCsvAsync();

            // Implementação atual faz Replace("\"", "''")
            Assert.Contains("Trigger ''A'' detected", csv);
        }

        [Fact]
        public async Task DataPortabilityService_ExportSensorReadingsCsv_ReturnsHeaderOnly_WhenNoReadings()
        {
            using var context = CreateContext();
            var service = new DataPortabilityService(context);

            var csv = await service.ExportSensorReadingsCsvAsync(null);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Single(lines);
            Assert.StartsWith("Id,SensorId,SensorName", lines[0]);
        }

        [Fact]
        public async Task DataPortabilityService_ImportSensorReadings_ReturnsNote_WhenEmptyInput()
        {
            using var context = CreateContext();
            var service = new DataPortabilityService(context);

            var summary = await service.ImportSensorReadingsAsync(Array.Empty<SensorReadingImportDto>());

            Assert.Equal(0, summary.Imported);
            Assert.Equal(0, summary.Skipped);
            Assert.Contains(summary.Notes, n => n.Contains("Nenhuma leitura fornecida"));
            Assert.Empty(context.SensorReadings);
        }

        [Fact]
        public async Task DataPortabilityService_ImportSensorReadings_DefaultsNullValueAndTimestamp()
        {
            using var context = CreateContext();
            context.Sensors.Add(new Sensor { Id = 5, Name = "Temp", BuildingId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new DataPortabilityService(context);

            var before = DateTime.UtcNow.AddSeconds(-2);
            var summary = await service.ImportSensorReadingsAsync(new[]
            {
                new SensorReadingImportDto { SensorId = 5, Value = null, Timestamp = null }
            });
            var after = DateTime.UtcNow.AddSeconds(2);

            Assert.Equal(1, summary.Imported);
            Assert.Equal(0, summary.Skipped);

            var reading = await context.SensorReadings.FirstAsync();
            Assert.Equal(0, reading.Value);
            Assert.True(reading.Timestamp >= before && reading.Timestamp <= after);
        }
    }
}
