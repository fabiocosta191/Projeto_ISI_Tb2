using System;
using SafeHome.API.DTOs;
using SafeHome.API.Services;
using Xunit;

namespace SafeHome.Tests
{
    public class ReportingAndPortabilityEdgeTests
    {
        [Fact]
        public async Task ReportingService_ComputesInactiveSensorsCorrectly()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("A");
            await db.InsertSensorAsync(buildingId, name: "S1", type: "S1", isActive: true);
            await db.InsertSensorAsync(buildingId, name: "S2", type: "S2", isActive: false);
            await db.InsertSensorAsync(buildingId, name: "S3", type: "S3", isActive: false);

            var service = new ReportingService(db.ConnectionFactory);
            var snapshot = await service.GetDashboardAsync();

            Assert.Equal(3, snapshot.TotalSensors);
            Assert.Equal(1, snapshot.ActiveSensors);
            Assert.Equal(2, snapshot.InactiveSensors);
        }

        [Fact]
        public async Task ReportingService_BuildingsOrderedByOpenIncidentsThenSensorCount()
        {
            await using var db = await TestDatabase.CreateAsync();

            var buildingAId = await db.InsertBuildingAsync("A");
            var buildingBId = await db.InsertBuildingAsync("B");

            // A: 2 open incidents, 1 sensor
            await db.InsertSensorAsync(buildingAId, name: "A1", type: "A1", isActive: true);
            await db.InsertIncidentAsync(buildingAId, type: "Fire", status: "Open");
            await db.InsertIncidentAsync(buildingAId, type: "Leak", status: "Open");

            // B: 1 open incident, 5 sensors
            await db.InsertSensorAsync(buildingBId, name: "B1", type: "B1", isActive: true);
            await db.InsertSensorAsync(buildingBId, name: "B2", type: "B2", isActive: true);
            await db.InsertSensorAsync(buildingBId, name: "B3", type: "B3", isActive: true);
            await db.InsertSensorAsync(buildingBId, name: "B4", type: "B4", isActive: true);
            await db.InsertSensorAsync(buildingBId, name: "B5", type: "B5", isActive: true);
            await db.InsertIncidentAsync(buildingBId, type: "Power", status: "Open");

            var service = new ReportingService(db.ConnectionFactory);
            var snapshot = await service.GetDashboardAsync();

            Assert.NotEmpty(snapshot.Buildings);
            Assert.Equal(buildingAId, snapshot.Buildings[0].BuildingId);
        }

        [Fact]
        public async Task ReportingService_ExportAlertsCsv_ReturnsHeaderOnly_WhenNoAlerts()
        {
            await using var db = await TestDatabase.CreateAsync();
            var service = new ReportingService(db.ConnectionFactory);

            var csv = await service.ExportAlertsCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Single(lines);
            Assert.StartsWith("Id,Timestamp,Severity", lines[0]);
        }

        [Fact]
        public async Task ReportingService_ExportIncidentsCsv_EscapesQuotesInDescription()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            await db.InsertIncidentAsync(
                buildingId,
                type: "Fire",
                status: "Open",
                severity: "High",
                description: "Trigger \"A\" detected");

            var service = new ReportingService(db.ConnectionFactory);
            var csv = await service.ExportIncidentsCsvAsync();

            Assert.Contains("Trigger ''A'' detected", csv);
        }

        [Fact]
        public async Task DataPortabilityService_ExportSensorReadingsCsv_ReturnsHeaderOnly_WhenNoReadings()
        {
            await using var db = await TestDatabase.CreateAsync();
            var service = new DataPortabilityService(db.ConnectionFactory);

            var csv = await service.ExportSensorReadingsCsvAsync(null);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Single(lines);
            Assert.StartsWith("Id,SensorId,SensorName", lines[0]);
        }

        [Fact]
        public async Task DataPortabilityService_ImportSensorReadings_ReturnsNote_WhenEmptyInput()
        {
            await using var db = await TestDatabase.CreateAsync();
            var service = new DataPortabilityService(db.ConnectionFactory);

            var summary = await service.ImportSensorReadingsAsync(Array.Empty<SensorReadingImportDto>());

            Assert.Equal(0, summary.Imported);
            Assert.Equal(0, summary.Skipped);
            Assert.Contains(summary.Notes, n => n.Contains("Nenhuma leitura fornecida"));
            Assert.Equal(0, await db.GetCountAsync("SensorReadings"));
        }

        [Fact]
        public async Task DataPortabilityService_ImportSensorReadings_DefaultsNullValueAndTimestamp()
        {
            await using var db = await TestDatabase.CreateAsync();
            var buildingId = await db.InsertBuildingAsync("HQ");
            var sensorId = await db.InsertSensorAsync(buildingId, name: "Temp", type: "Temp", isActive: true);

            var service = new DataPortabilityService(db.ConnectionFactory);

            var before = DateTime.UtcNow.AddSeconds(-2);
            var summary = await service.ImportSensorReadingsAsync(new[]
            {
                new SensorReadingImportDto { SensorId = sensorId, Value = null, Timestamp = null }
            });
            var after = DateTime.UtcNow.AddSeconds(2);

            Assert.Equal(1, summary.Imported);
            Assert.Equal(0, summary.Skipped);

            var reading = await db.GetFirstSensorReadingAsync();
            Assert.Equal(0, reading.Value);
            Assert.True(reading.Timestamp >= before && reading.Timestamp <= after);
        }
    }
}
