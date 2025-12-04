using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SafeHome.Data.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Inicializar para evitar avisos
        public string Type { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        [JsonIgnore]
        public List<SensorReading>? Readings { get; set; } // Adicionado o '?'
    }
}