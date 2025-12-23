using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class AlertService : IAlertService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AlertService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<Alert>> GetAllAsync()
        {
            const string sql = @"SELECT a.Id, a.Timestamp, a.Message, a.Severity, a.IsResolved, a.SensorId,
                                        s.Id AS Sensor_Id, s.Name, s.Type, s.IsActive, s.BuildingId
                                 FROM Alerts a
                                 INNER JOIN Sensors s ON a.SensorId = s.Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var alerts = new List<Alert>();

            while (await reader.ReadAsync())
            {
                alerts.Add(MapAlertWithSensor(reader));
            }

            return alerts;
        }

        public async Task<Alert?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT a.Id, a.Timestamp, a.Message, a.Severity, a.IsResolved, a.SensorId,
                                        s.Id AS Sensor_Id, s.Name, s.Type, s.IsActive, s.BuildingId
                                 FROM Alerts a
                                 INNER JOIN Sensors s ON a.SensorId = s.Id
                                 WHERE a.Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapAlertWithSensor(reader);
        }

        public async Task<Alert> CreateAsync(Alert alert)
        {
            const string sql = @"INSERT INTO Alerts (Timestamp, Message, Severity, IsResolved, SensorId)
                                 OUTPUT INSERTED.Id
                                 VALUES (@Timestamp, @Message, @Severity, @IsResolved, @SensorId)";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Timestamp", alert.Timestamp);
            command.Parameters.AddWithValue("@Message", alert.Message);
            command.Parameters.AddWithValue("@Severity", alert.Severity);
            command.Parameters.AddWithValue("@IsResolved", alert.IsResolved);
            command.Parameters.AddWithValue("@SensorId", alert.SensorId);
            alert.Id = (int)await command.ExecuteScalarAsync();
            return alert;
        }

        public async Task<bool> UpdateAsync(int id, Alert alert)
        {
            const string sql = @"UPDATE Alerts
                                 SET Message = @Message,
                                     Severity = @Severity,
                                     IsResolved = @IsResolved,
                                     Timestamp = @Timestamp,
                                     SensorId = @SensorId
                                 WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Message", alert.Message);
            command.Parameters.AddWithValue("@Severity", alert.Severity);
            command.Parameters.AddWithValue("@IsResolved", alert.IsResolved);
            command.Parameters.AddWithValue("@Timestamp", alert.Timestamp);
            command.Parameters.AddWithValue("@SensorId", alert.SensorId);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Alerts WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static Alert MapAlertWithSensor(SqlDataReader reader)
        {
            var sensor = new Sensor
            {
                Id = reader.GetInt32(reader.GetOrdinal("Sensor_Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId"))
            };

            return new Alert
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                Message = reader.GetString(reader.GetOrdinal("Message")),
                Severity = reader.GetString(reader.GetOrdinal("Severity")),
                IsResolved = reader.GetBoolean(reader.GetOrdinal("IsResolved")),
                SensorId = reader.GetInt32(reader.GetOrdinal("SensorId")),
                Sensor = sensor
            };
        }
    }
}
