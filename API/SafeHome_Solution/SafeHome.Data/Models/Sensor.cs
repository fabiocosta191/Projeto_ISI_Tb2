using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SafeHome.Data.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Ex: Smoke, Flood
        public bool IsActive { get; set; }

        // Relação com Edifício (Novo)
        public int BuildingId { get; set; }
        [JsonIgnore]
        public Building? Building { get; set; }

        [JsonIgnore]
        public List<SensorReading>? Readings { get; set; }
    }
}