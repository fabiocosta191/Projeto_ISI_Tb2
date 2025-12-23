using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class SensorReadingService : ISensorReadingService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorReadingService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<SensorReading>> GetAllAsync()
        {
            const string sql = @"SELECT sr.Id, sr.Value, sr.Timestamp, sr.SensorId,
                                        s.Id AS Sensor_Id, s.Name, s.Type, s.IsActive, s.BuildingId
                                 FROM SensorReadings sr
                                 INNER JOIN Sensors s ON sr.SensorId = s.Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var readings = new List<SensorReading>();

            while (await reader.ReadAsync())
            {
                readings.Add(MapReadingWithSensor(reader));
            }

            return readings;
        }

        public async Task<SensorReading?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT sr.Id, sr.Value, sr.Timestamp, sr.SensorId,
                                        s.Id AS Sensor_Id, s.Name, s.Type, s.IsActive, s.BuildingId
                                 FROM SensorReadings sr
                                 INNER JOIN Sensors s ON sr.SensorId = s.Id
                                 WHERE sr.Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapReadingWithSensor(reader);
        }

        public async Task<SensorReading> CreateAsync(SensorReading reading)
        {
            const string sql = "INSERT INTO SensorReadings (Value, Timestamp, SensorId) OUTPUT INSERTED.Id VALUES (@Value, @Timestamp, @SensorId)";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Value", reading.Value);
            command.Parameters.AddWithValue("@Timestamp", reading.Timestamp);
            command.Parameters.AddWithValue("@SensorId", reading.SensorId);
            reading.Id = (int)await command.ExecuteScalarAsync();
            return reading;
        }

        public async Task<bool> UpdateAsync(int id, SensorReading reading)
        {
            const string sql = "UPDATE SensorReadings SET Value = @Value, Timestamp = @Timestamp, SensorId = @SensorId WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Value", reading.Value);
            command.Parameters.AddWithValue("@Timestamp", reading.Timestamp);
            command.Parameters.AddWithValue("@SensorId", reading.SensorId);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM SensorReadings WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static SensorReading MapReadingWithSensor(SqlDataReader reader)
        {
            var sensor = new Sensor
            {
                Id = reader.GetInt32(reader.GetOrdinal("Sensor_Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId"))
            };

            return new SensorReading
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Value = reader.GetDouble(reader.GetOrdinal("Value")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                SensorId = reader.GetInt32(reader.GetOrdinal("SensorId")),
                Sensor = sensor
            };
        }
    }
}
