using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.API.DTOs;

namespace SafeHome.API.Services
{
    public class DataPortabilityService : IDataPortabilityService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DataPortabilityService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<string> ExportSensorReadingsCsvAsync(int? sensorId = null)
        {
            const string sql = @"SELECT sr.Id, sr.SensorId, sr.Value, sr.Timestamp,
                                        s.Name AS SensorName, b.Name AS BuildingName
                                 FROM SensorReadings sr
                                 INNER JOIN Sensors s ON sr.SensorId = s.Id
                                 LEFT JOIN Buildings b ON s.BuildingId = b.Id
                                 WHERE (@SensorId IS NULL OR sr.SensorId = @SensorId)
                                 ORDER BY sr.Timestamp DESC";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@SensorId", SqlDbType.Int)
            {
                Value = sensorId.HasValue ? sensorId.Value : DBNull.Value
            });
            await using var reader = await command.ExecuteReaderAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,SensorId,SensorName,Building,Value,Timestamp");

            while (await reader.ReadAsync())
            {
                var sensorNameOrdinal = reader.GetOrdinal("SensorName");
                var buildingNameOrdinal = reader.GetOrdinal("BuildingName");
                var sensorName = reader.IsDBNull(sensorNameOrdinal) ? "" : reader.GetString(sensorNameOrdinal);
                var buildingName = reader.IsDBNull(buildingNameOrdinal) ? "" : reader.GetString(buildingNameOrdinal);
                var timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"));

                csv.AppendLine($"{reader.GetInt32(reader.GetOrdinal("Id"))},{reader.GetInt32(reader.GetOrdinal("SensorId"))},\"{sensorName}\",\"{buildingName}\",{reader.GetDouble(reader.GetOrdinal("Value"))},{timestamp:O}");
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
            var existingSensors = new HashSet<int>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            if (sensorIds.Count > 0)
            {
                var parameterNames = sensorIds.Select((_, index) => $"@Id{index}").ToList();
                var sql = $"SELECT Id FROM Sensors WHERE Id IN ({string.Join(", ", parameterNames)})";
                await using var command = new SqlCommand(sql, connection);

                for (var i = 0; i < sensorIds.Count; i++)
                {
                    command.Parameters.AddWithValue(parameterNames[i], sensorIds[i]);
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingSensors.Add(reader.GetInt32(reader.GetOrdinal("Id")));
                }
            }

            using var transaction = connection.BeginTransaction();
            const string insertSql = "INSERT INTO SensorReadings (SensorId, Value, Timestamp) VALUES (@SensorId, @Value, @Timestamp)";
            await using var insertCommand = new SqlCommand(insertSql, connection, transaction);
            var sensorIdParam = insertCommand.Parameters.Add("@SensorId", SqlDbType.Int);
            var valueParam = insertCommand.Parameters.Add("@Value", SqlDbType.Float);
            var timestampParam = insertCommand.Parameters.Add("@Timestamp", SqlDbType.DateTime2);

            foreach (var readingDto in readingsList)
            {
                if (!existingSensors.Contains(readingDto.SensorId))
                {
                    summary.Skipped++;
                    summary.Notes.Add($"Sensor {readingDto.SensorId} inexistente. Leitura ignorada.");
                    continue;
                }

                sensorIdParam.Value = readingDto.SensorId;
                valueParam.Value = readingDto.Value ?? 0;
                timestampParam.Value = readingDto.Timestamp ?? DateTime.UtcNow;

                await insertCommand.ExecuteNonQueryAsync();
                summary.Imported++;
            }

            transaction.Commit();
            return summary;
        }
    }
}
