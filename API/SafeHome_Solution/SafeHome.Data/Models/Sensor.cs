using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SafeHome.Data.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Ex: "Smoke", "Temperature"
        public bool IsActive { get; set; }

        [JsonIgnore]
        public List<SensorReading> Readings { get; set; }
    }
}