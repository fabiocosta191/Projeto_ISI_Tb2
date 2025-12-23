using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SafeHome.API.Data;

namespace SafeHome.Tests
{
    public sealed class TestDatabase : IAsyncDisposable
    {
        private const string LocalDbDataSource = @"(localdb)\MSSQLLocalDB";
        private readonly string _databaseName;
        private readonly string _masterConnectionString;

        private TestDatabase(string databaseName, string masterConnectionString, string connectionString)
        {
            _databaseName = databaseName;
            _masterConnectionString = masterConnectionString;
            ConnectionString = connectionString;
            ConnectionFactory = new SqlConnectionFactory(connectionString);
        }

        public string ConnectionString { get; }
        public IDbConnectionFactory ConnectionFactory { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databaseName = $"SafeHomeTests_{Guid.NewGuid():N}";
            var masterBuilder = new SqlConnectionStringBuilder
            {
                DataSource = LocalDbDataSource,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                Pooling = false
            };
            var dbBuilder = new SqlConnectionStringBuilder
            {
                DataSource = LocalDbDataSource,
                InitialCatalog = databaseName,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                Pooling = false
            };

            await using (var connection = new SqlConnection(masterBuilder.ConnectionString))
            {
                await connection.OpenAsync();
                await using var createCommand = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection);
                await createCommand.ExecuteNonQueryAsync();
            }

            var database = new TestDatabase(databaseName, masterBuilder.ConnectionString, dbBuilder.ConnectionString);
            await database.CreateSchemaAsync();
            return database;
        }

        public async ValueTask DisposeAsync()
        {
            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            var dropSql = $@"IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END";
            await using var command = new SqlCommand(dropSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        public Task<int> InsertBuildingAsync(
            string name,
            string address = "Test Address",
            double latitude = 0,
            double longitude = 0,
            string riskType = "None")
        {
            const string sql = @"INSERT INTO Buildings (Name, Address, Latitude, Longitude, RiskType)
OUTPUT INSERTED.Id
VALUES (@Name, @Address, @Latitude, @Longitude, @RiskType)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@Name", name),
                new SqlParameter("@Address", address),
                new SqlParameter("@Latitude", latitude),
                new SqlParameter("@Longitude", longitude),
                new SqlParameter("@RiskType", riskType));
        }

        public Task<int> InsertSensorAsync(
            int buildingId,
            string name = "Sensor",
            string type = "Generic",
            bool isActive = true)
        {
            const string sql = @"INSERT INTO Sensors (Name, Type, IsActive, BuildingId)
OUTPUT INSERTED.Id
VALUES (@Name, @Type, @IsActive, @BuildingId)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@Name", name),
                new SqlParameter("@Type", type),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@BuildingId", buildingId));
        }

        public Task<int> InsertSensorReadingAsync(
            int sensorId,
            double value,
            DateTime? timestamp = null)
        {
            const string sql = @"INSERT INTO SensorReadings (SensorId, Value, Timestamp)
OUTPUT INSERTED.Id
VALUES (@SensorId, @Value, @Timestamp)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@SensorId", sensorId),
                new SqlParameter("@Value", value),
                new SqlParameter("@Timestamp", timestamp ?? DateTime.UtcNow));
        }

        public Task<int> InsertAlertAsync(
            int sensorId,
            string message = "Alert",
            string severity = "High",
            bool isResolved = false,
            DateTime? timestamp = null)
        {
            const string sql = @"INSERT INTO Alerts (Timestamp, Message, Severity, IsResolved, SensorId)
OUTPUT INSERTED.Id
VALUES (@Timestamp, @Message, @Severity, @IsResolved, @SensorId)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@Timestamp", timestamp ?? DateTime.UtcNow),
                new SqlParameter("@Message", message),
                new SqlParameter("@Severity", severity),
                new SqlParameter("@IsResolved", isResolved),
                new SqlParameter("@SensorId", sensorId));
        }

        public Task<int> InsertIncidentAsync(
            int buildingId,
            string type = "Fire",
            string status = "Open",
            string severity = "High",
            string description = "Test incident",
            DateTime? startedAt = null,
            DateTime? endedAt = null)
        {
            const string sql = @"INSERT INTO Incidents (Type, StartedAt, EndedAt, Severity, Status, Description, BuildingId)
OUTPUT INSERTED.Id
VALUES (@Type, @StartedAt, @EndedAt, @Severity, @Status, @Description, @BuildingId)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@Type", type),
                new SqlParameter("@StartedAt", startedAt ?? DateTime.UtcNow),
                new SqlParameter("@EndedAt", (object?)endedAt ?? DBNull.Value),
                new SqlParameter("@Severity", severity),
                new SqlParameter("@Status", status),
                new SqlParameter("@Description", description),
                new SqlParameter("@BuildingId", buildingId));
        }

        public Task<int> InsertUserAsync(string username, string passwordHash, string role)
        {
            const string sql = @"INSERT INTO Users (Username, PasswordHash, Role)
OUTPUT INSERTED.Id
VALUES (@Username, @PasswordHash, @Role)";
            return ExecuteScalarAsync<int>(sql,
                new SqlParameter("@Username", username),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@Role", role));
        }

        public Task<int> GetCountAsync(string tableName)
        {
            var sql = $"SELECT COUNT(*) FROM {tableName}";
            return ExecuteScalarAsync<int>(sql);
        }

        public async Task<(double Value, DateTime Timestamp)> GetFirstSensorReadingAsync()
        {
            const string sql = "SELECT TOP 1 Value, Timestamp FROM SensorReadings";
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("No sensor readings found.");
            }

            return (reader.GetDouble(0), reader.GetDateTime(1));
        }

        public async Task<string?> GetUserPasswordHashAsync(string username)
        {
            const string sql = "SELECT PasswordHash FROM Users WHERE Username = @Username";
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);
            var result = await command.ExecuteScalarAsync();
            return result == null ? null : Convert.ToString(result);
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        private async Task CreateSchemaAsync()
        {
            const string sql = @"
CREATE TABLE Buildings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(200) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    RiskType NVARCHAR(100) NOT NULL
);

CREATE TABLE Sensors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Type NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL,
    BuildingId INT NOT NULL,
    CONSTRAINT FK_Sensors_Buildings FOREIGN KEY (BuildingId) REFERENCES Buildings(Id)
);

CREATE TABLE SensorReadings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SensorId INT NOT NULL,
    Value FLOAT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    CONSTRAINT FK_SensorReadings_Sensors FOREIGN KEY (SensorId) REFERENCES Sensors(Id)
);

CREATE TABLE Alerts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Severity NVARCHAR(50) NOT NULL,
    IsResolved BIT NOT NULL,
    SensorId INT NOT NULL,
    CONSTRAINT FK_Alerts_Sensors FOREIGN KEY (SensorId) REFERENCES Sensors(Id)
);

CREATE TABLE Incidents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Type NVARCHAR(100) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    EndedAt DATETIME2 NULL,
    Severity NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    BuildingId INT NOT NULL,
    CONSTRAINT FK_Incidents_Buildings FOREIGN KEY (BuildingId) REFERENCES Buildings(Id)
);

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL
);";

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
