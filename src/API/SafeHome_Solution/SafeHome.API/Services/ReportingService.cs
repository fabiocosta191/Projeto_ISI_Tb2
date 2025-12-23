using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.API.DTOs;

namespace SafeHome.API.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReportingService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<DashboardSnapshotDto> GetDashboardAsync()
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var snapshot = new DashboardSnapshotDto
            {
                TotalBuildings = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Buildings"),
                TotalSensors = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Sensors"),
                ActiveSensors = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Sensors WHERE IsActive = 1"),
                OpenIncidents = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Incidents WHERE Status <> 'Resolved'"),
                ResolvedIncidents = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Incidents WHERE Status = 'Resolved'"),
                OpenAlerts = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Alerts WHERE IsResolved = 0"),
                ResolvedAlerts = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM Alerts WHERE IsResolved = 1"),
                GeneratedAtUtc = DateTime.UtcNow
            };

            snapshot.InactiveSensors = snapshot.TotalSensors - snapshot.ActiveSensors;

            const string buildingSql = @"SELECT b.Id, b.Name,
                                                (SELECT COUNT(*) FROM Sensors s WHERE s.BuildingId = b.Id) AS SensorCount,
                                                (SELECT COUNT(*) FROM Incidents i WHERE i.BuildingId = b.Id AND i.Status <> 'Resolved') AS OpenIncidents
                                         FROM Buildings b
                                         ORDER BY OpenIncidents DESC, SensorCount DESC";
            await using var buildingCommand = new SqlCommand(buildingSql, connection);
            await using var reader = await buildingCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                snapshot.Buildings.Add(new BuildingLoadDto
                {
                    BuildingId = reader.GetInt32(reader.GetOrdinal("Id")),
                    BuildingName = reader.GetString(reader.GetOrdinal("Name")),
                    SensorCount = reader.GetInt32(reader.GetOrdinal("SensorCount")),
                    OpenIncidents = reader.GetInt32(reader.GetOrdinal("OpenIncidents"))
                });
            }

            return snapshot;
        }

        public async Task<string> ExportAlertsCsvAsync()
        {
            const string sql = @"SELECT a.Id, a.Timestamp, a.Severity, a.IsResolved, a.SensorId,
                                        s.Name AS SensorName, b.Name AS BuildingName, a.Message
                                 FROM Alerts a
                                 INNER JOIN Sensors s ON a.SensorId = s.Id
                                 LEFT JOIN Buildings b ON s.BuildingId = b.Id
                                 ORDER BY a.Timestamp DESC";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,Severity,IsResolved,SensorId,SensorName,Building,Message");

            while (await reader.ReadAsync())
            {
                var sensorNameOrdinal = reader.GetOrdinal("SensorName");
                var buildingNameOrdinal = reader.GetOrdinal("BuildingName");
                var messageOrdinal = reader.GetOrdinal("Message");
                var sensorName = reader.IsDBNull(sensorNameOrdinal) ? "" : reader.GetString(sensorNameOrdinal);
                var buildingName = reader.IsDBNull(buildingNameOrdinal) ? "" : reader.GetString(buildingNameOrdinal);
                var message = reader.IsDBNull(messageOrdinal) ? "" : reader.GetString(messageOrdinal);

                csv.AppendLine(
                    $"{reader.GetInt32(reader.GetOrdinal("Id"))},{reader.GetDateTime(reader.GetOrdinal("Timestamp")):O},{reader.GetString(reader.GetOrdinal("Severity"))},{reader.GetBoolean(reader.GetOrdinal("IsResolved"))},{reader.GetInt32(reader.GetOrdinal("SensorId"))},\"{sensorName}\",\"{buildingName}\",\"{message.Replace("\"", "''")}\"");
            }

            return csv.ToString();
        }

        public async Task<string> ExportIncidentsCsvAsync()
        {
            const string sql = @"SELECT i.Id, i.Type, i.Severity, i.Status, i.StartedAt, i.EndedAt, i.Description,
                                        b.Name AS BuildingName
                                 FROM Incidents i
                                 LEFT JOIN Buildings b ON i.BuildingId = b.Id
                                 ORDER BY i.StartedAt DESC";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Type,Severity,Status,Building,StartedAt,EndedAt,Description");

            while (await reader.ReadAsync())
            {
                var buildingNameOrdinal = reader.GetOrdinal("BuildingName");
                var endedAtOrdinal = reader.GetOrdinal("EndedAt");
                var descriptionOrdinal = reader.GetOrdinal("Description");
                var buildingName = reader.IsDBNull(buildingNameOrdinal) ? "" : reader.GetString(buildingNameOrdinal);
                var endedAt = reader.IsDBNull(endedAtOrdinal) ? "" : reader.GetDateTime(endedAtOrdinal).ToString("O");
                var description = reader.IsDBNull(descriptionOrdinal) ? "" : reader.GetString(descriptionOrdinal);

                csv.AppendLine(
                    $"{reader.GetInt32(reader.GetOrdinal("Id"))},{reader.GetString(reader.GetOrdinal("Type"))},{reader.GetString(reader.GetOrdinal("Severity"))},{reader.GetString(reader.GetOrdinal("Status"))},\"{buildingName}\",{reader.GetDateTime(reader.GetOrdinal("StartedAt")):O},{endedAt},\"{description.Replace("\"", "''")}\"");
            }

            return csv.ToString();
        }

        private static async Task<int> ExecuteCountAsync(SqlConnection connection, string sql)
        {
            await using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
