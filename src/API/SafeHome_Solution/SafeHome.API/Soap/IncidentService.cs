using Microsoft.Data.SqlClient;
using SafeHome.API.Data;
using SafeHome.Data.Models;

namespace SafeHome.API.Soap
{
    public class IncidentService : IIncidentService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        // Injetar a Base de Dados
        public IncidentService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<string> ReportIncident(string type, string description, int buildingId, string severity)
        {
            var incident = await CreateIncident(new Incident
            {
                Type = type,
                Description = description,
                BuildingId = buildingId,
                StartedAt = DateTime.UtcNow,
                Status = "Reported",
                Severity = severity
            });

            return $"Incidente recebido pela Prote\u2021\u00C6o Civil. ID: {incident.Id}";
        }

        public async Task<List<Incident>> GetUnresolvedIncidents()
        {
            const string sql = @"SELECT i.Id, i.Type, i.StartedAt, i.EndedAt, i.Severity, i.Status, i.Description, i.BuildingId,
                                        b.Id AS Building_Id, b.Name, b.Address, b.Latitude, b.Longitude, b.RiskType
                                 FROM Incidents i
                                 LEFT JOIN Buildings b ON i.BuildingId = b.Id
                                 WHERE i.Status <> 'Resolved'";
            return await GetIncidentsAsync(sql, null);
        }

        public async Task<List<Incident>> GetAllIncidents()
        {
            const string sql = @"SELECT i.Id, i.Type, i.StartedAt, i.EndedAt, i.Severity, i.Status, i.Description, i.BuildingId,
                                        b.Id AS Building_Id, b.Name, b.Address, b.Latitude, b.Longitude, b.RiskType
                                 FROM Incidents i
                                 LEFT JOIN Buildings b ON i.BuildingId = b.Id";
            return await GetIncidentsAsync(sql, null);
        }

        public async Task<Incident?> GetIncidentById(int id)
        {
            const string sql = @"SELECT i.Id, i.Type, i.StartedAt, i.EndedAt, i.Severity, i.Status, i.Description, i.BuildingId,
                                        b.Id AS Building_Id, b.Name, b.Address, b.Latitude, b.Longitude, b.RiskType
                                 FROM Incidents i
                                 LEFT JOIN Buildings b ON i.BuildingId = b.Id
                                 WHERE i.Id = @Id";
            var incidents = await GetIncidentsAsync(sql, new SqlParameter("@Id", id));
            return incidents.FirstOrDefault();
        }

        public async Task<Incident> CreateIncident(Incident incident)
        {
            if (incident.StartedAt == default)
            {
                incident.StartedAt = DateTime.UtcNow;
            }

            const string sql = @"INSERT INTO Incidents (Type, StartedAt, EndedAt, Severity, Status, Description, BuildingId)
                                 OUTPUT INSERTED.Id
                                 VALUES (@Type, @StartedAt, @EndedAt, @Severity, @Status, @Description, @BuildingId)";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Type", incident.Type);
            command.Parameters.AddWithValue("@StartedAt", incident.StartedAt);
            command.Parameters.AddWithValue("@EndedAt", (object?)incident.EndedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@Severity", incident.Severity);
            command.Parameters.AddWithValue("@Status", incident.Status);
            command.Parameters.AddWithValue("@Description", incident.Description);
            command.Parameters.AddWithValue("@BuildingId", incident.BuildingId);
            incident.Id = (int)await command.ExecuteScalarAsync();
            return incident;
        }

        public async Task<bool> UpdateIncident(int id, Incident incident)
        {
            const string sql = @"UPDATE Incidents
                                 SET Type = @Type,
                                     Description = @Description,
                                     BuildingId = @BuildingId,
                                     StartedAt = @StartedAt,
                                     EndedAt = @EndedAt,
                                     Status = @Status,
                                     Severity = @Severity
                                 WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Type", incident.Type);
            command.Parameters.AddWithValue("@Description", incident.Description);
            command.Parameters.AddWithValue("@BuildingId", incident.BuildingId);
            command.Parameters.AddWithValue("@StartedAt", incident.StartedAt);
            command.Parameters.AddWithValue("@EndedAt", (object?)incident.EndedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", incident.Status);
            command.Parameters.AddWithValue("@Severity", incident.Severity);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteIncident(int id)
        {
            const string sql = "DELETE FROM Incidents WHERE Id = @Id";
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private async Task<List<Incident>> GetIncidentsAsync(string sql, SqlParameter? parameter)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            if (parameter != null)
            {
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync();
            var incidents = new List<Incident>();

            while (await reader.ReadAsync())
            {
                incidents.Add(MapIncidentWithBuilding(reader));
            }

            return incidents;
        }

        private static Incident MapIncidentWithBuilding(SqlDataReader reader)
        {
            var buildingNameOrdinal = reader.GetOrdinal("Name");
            Building? building = null;

            if (!reader.IsDBNull(buildingNameOrdinal))
            {
                building = new Building
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Building_Id")),
                    Name = reader.GetString(buildingNameOrdinal),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    Latitude = reader.GetDouble(reader.GetOrdinal("Latitude")),
                    Longitude = reader.GetDouble(reader.GetOrdinal("Longitude")),
                    RiskType = reader.GetString(reader.GetOrdinal("RiskType"))
                };
            }

            return new Incident
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                EndedAt = reader.IsDBNull(reader.GetOrdinal("EndedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("EndedAt")),
                Severity = reader.GetString(reader.GetOrdinal("Severity")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId")),
                Building = building
            };
        }
    }
}
