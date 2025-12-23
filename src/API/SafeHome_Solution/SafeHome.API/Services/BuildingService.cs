using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BuildingService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<Building>> GetAllBuildings()
        {
            const string sql = "SELECT Id, Name, Address, Latitude, Longitude, RiskType FROM Buildings";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var buildings = new List<Building>();

            while (await reader.ReadAsync())
            {
                buildings.Add(MapBuilding(reader));
            }

            return buildings;
        }

        public async Task<Building?> GetBuildingById(int id)
        {
            const string sql = "SELECT Id, Name, Address, Latitude, Longitude, RiskType FROM Buildings WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapBuilding(reader);
        }

        public async Task<Building> CreateBuilding(Building building)
        {
            const string sql = @"INSERT INTO Buildings (Name, Address, Latitude, Longitude, RiskType)
                                 OUTPUT INSERTED.Id
                                 VALUES (@Name, @Address, @Latitude, @Longitude, @RiskType)";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", building.Name);
            command.Parameters.AddWithValue("@Address", building.Address);
            command.Parameters.AddWithValue("@Latitude", building.Latitude);
            command.Parameters.AddWithValue("@Longitude", building.Longitude);
            command.Parameters.AddWithValue("@RiskType", building.RiskType);
            building.Id = (int)await command.ExecuteScalarAsync();
            return building;
        }

        public async Task<bool> UpdateBuilding(int id, Building building)
        {
            if (id != building.Id) return false;

            const string sql = @"UPDATE Buildings
                                 SET Name = @Name,
                                     Address = @Address,
                                     Latitude = @Latitude,
                                     Longitude = @Longitude,
                                     RiskType = @RiskType
                                 WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", building.Name);
            command.Parameters.AddWithValue("@Address", building.Address);
            command.Parameters.AddWithValue("@Latitude", building.Latitude);
            command.Parameters.AddWithValue("@Longitude", building.Longitude);
            command.Parameters.AddWithValue("@RiskType", building.RiskType);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteBuilding(int id)
        {
            const string existsSql = "SELECT 1 FROM Buildings WHERE Id = @Id";
            const string deleteIncidentsSql = "DELETE FROM Incidents WHERE BuildingId = @Id";
            const string deleteReadingsSql = "DELETE FROM SensorReadings WHERE SensorId IN (SELECT Id FROM Sensors WHERE BuildingId = @Id)";
            const string deleteAlertsSql = "DELETE FROM Alerts WHERE SensorId IN (SELECT Id FROM Sensors WHERE BuildingId = @Id)";
            const string deleteSensorsSql = "DELETE FROM Sensors WHERE BuildingId = @Id";
            const string deleteBuildingSql = "DELETE FROM Buildings WHERE Id = @Id";

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

            await using (var deleteIncidentsCommand = new SqlCommand(deleteIncidentsSql, connection, transaction))
            {
                deleteIncidentsCommand.Parameters.AddWithValue("@Id", id);
                await deleteIncidentsCommand.ExecuteNonQueryAsync();
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

            await using (var deleteSensorsCommand = new SqlCommand(deleteSensorsSql, connection, transaction))
            {
                deleteSensorsCommand.Parameters.AddWithValue("@Id", id);
                await deleteSensorsCommand.ExecuteNonQueryAsync();
            }

            await using (var deleteBuildingCommand = new SqlCommand(deleteBuildingSql, connection, transaction))
            {
                deleteBuildingCommand.Parameters.AddWithValue("@Id", id);
                await deleteBuildingCommand.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            return true;
        }

        private static Building MapBuilding(SqlDataReader reader)
        {
            return new Building
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Address = reader.GetString(reader.GetOrdinal("Address")),
                Latitude = reader.GetDouble(reader.GetOrdinal("Latitude")),
                Longitude = reader.GetDouble(reader.GetOrdinal("Longitude")),
                RiskType = reader.GetString(reader.GetOrdinal("RiskType"))
            };
        }
    }
}
