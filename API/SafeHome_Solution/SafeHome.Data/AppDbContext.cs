using SafeHome.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SafeHome.Data
{
    /// <summary>
    /// Armazena os dados da aplicação em memória, sem dependências de Entity Framework.
    /// </summary>
    public class AppDbContext
    {
        public List<User> Users { get; } = new();
        public List<Sensor> Sensors { get; } = new();
        public List<SensorReading> SensorReadings { get; } = new();
        public List<Building> Buildings { get; } = new();
        public List<Incident> Incidents { get; } = new();
        public List<Alert> Alerts { get; } = new();

        public Task SaveChangesAsync()
        {
            AssignIds(Users);
            AssignIds(Sensors);
            AssignIds(SensorReadings);
            AssignIds(Buildings);
            AssignIds(Incidents);
            AssignIds(Alerts);

            foreach (var sensor in Sensors)
            {
                if (sensor.BuildingId == 0 && sensor.Building != null)
                {
                    sensor.BuildingId = sensor.Building.Id;
                }
            }

            foreach (var incident in Incidents)
            {
                if (incident.BuildingId == 0 && incident.Building != null)
                {
                    incident.BuildingId = incident.Building.Id;
                }
            }

            foreach (var alert in Alerts)
            {
                if (alert.SensorId == 0 && alert.Sensor != null)
                {
                    alert.SensorId = alert.Sensor.Id;
                }
            }

            return Task.CompletedTask;
        }

        private static void AssignIds<T>(List<T> entities)
        {
            var idProperty = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty == null || idProperty.PropertyType != typeof(int)) return;

            var currentMax = entities
                .Select(e => (int?)idProperty.GetValue(e) ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var entity in entities.Where(e => ((int?)idProperty.GetValue(e) ?? 0) == 0))
            {
                currentMax++;
                idProperty.SetValue(entity, currentMax);
            }
        }
    }
}
