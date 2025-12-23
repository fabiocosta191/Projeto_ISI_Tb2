using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class SensorService : ISensorService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SensorService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<Sensor>> GetAllSensors()
        {
            const string sql = "SELECT Id, Name, Type, IsActive, BuildingId FROM Sensors";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var sensors = new List<Sensor>();

            while (await reader.ReadAsync())
            {
                sensors.Add(MapSensor(reader));
            }

            return sensors;
        }

        public async Task<Sensor?> GetSensorById(int id)
        {
            const string sql = "SELECT Id, Name, Type, IsActive, BuildingId FROM Sensors WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapSensor(reader);
        }

        public async Task<Sensor> CreateSensor(Sensor sensor)
        {
            const string sql = "INSERT INTO Sensors (Name, Type, IsActive, BuildingId) OUTPUT INSERTED.Id VALUES (@Name, @Type, @IsActive, @BuildingId)";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", sensor.Name);
            command.Parameters.AddWithValue("@Type", sensor.Type);
            command.Parameters.AddWithValue("@IsActive", sensor.IsActive);
            command.Parameters.AddWithValue("@BuildingId", sensor.BuildingId);

            try
            {
                sensor.Id = (int)await command.ExecuteScalarAsync();
                return sensor;
            }
            catch (SqlException ex) when (ex.Message.Contains("FOREIGN KEY"))
            {
                throw new Exception("Database error.", ex);
            }
        }

        public async Task<bool> UpdateSensor(int id, Sensor sensor)
        {
            const string sql = "UPDATE Sensors SET Name = @Name, Type = @Type, IsActive = @IsActive, BuildingId = @BuildingId WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", sensor.Name);
            command.Parameters.AddWithValue("@Type", sensor.Type);
            command.Parameters.AddWithValue("@IsActive", sensor.IsActive);
            command.Parameters.AddWithValue("@BuildingId", sensor.BuildingId);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteSensor(int id)
        {
            const string existsSql = "SELECT 1 FROM Sensors WHERE Id = @Id";
            const string deleteReadingsSql = "DELETE FROM SensorReadings WHERE SensorId = @Id";
            const string deleteAlertsSql = "DELETE FROM Alerts WHERE SensorId = @Id";
            const string deleteSensorSql = "DELETE FROM Sensors WHERE Id = @Id";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            await using (var existsCommand = new SqlCommand(existsSql, connection, transaction))
            {
                existsCommand.Parameters.AddWithValue("@Id", id);
                var exists = await existsCommand.ExecuteScalarAsync();
                if (exists == null)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            await using (var deleteReadingsCommand = new SqlCommand(deleteReadingsSql, connection, transaction))
            {
                deleteReadingsCommand.Parameters.AddWithValue("@Id", id);
                await deleteReadingsCommand.ExecuteNonQueryAsync();
            }

            await using (var deleteAlertsCommand = new SqlCommand(deleteAlertsSql, connection, transaction))
            {
                deleteAlertsCommand.Parameters.AddWithValue("@Id", id);
                await deleteAlertsCommand.ExecuteNonQueryAsync();
            }

            await using (var deleteSensorCommand = new SqlCommand(deleteSensorSql, connection, transaction))
            {
                deleteSensorCommand.Parameters.AddWithValue("@Id", id);
                await deleteSensorCommand.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            return true;
        }

        private static Sensor MapSensor(SqlDataReader reader)
        {
            return new Sensor
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId"))
            };
        }
    }
}
