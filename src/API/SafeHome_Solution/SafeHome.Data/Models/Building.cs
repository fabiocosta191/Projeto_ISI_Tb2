using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SafeHome.Data.Models
{
    public class Building
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Ex: "Bloco A"
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string RiskType { get; set; } = "None"; // Ex: Incêndio, Cheia

        [JsonIgnore]
        public List<Sensor>? Sensors { get; set; }

        [JsonIgnore]
        public List<Incident>? Incidents { get; set; }
    }
}