using System;

namespace SafeHome.Data.Models
{
    public class Alert
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // Warning, Critical
        public bool IsResolved { get; set; }

        // Que sensor disparou?
        public int SensorId { get; set; }
        public Sensor? Sensor { get; set; }
    }
}