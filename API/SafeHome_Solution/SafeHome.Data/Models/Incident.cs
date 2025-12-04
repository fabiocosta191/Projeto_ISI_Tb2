using System;

namespace SafeHome.Data.Models
{
    public class Incident
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // Fire, Flood
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public string Severity { get; set; } = "Low"; // Low, Medium, High
        public string Status { get; set; } = "Reported"; // Reported, Resolved
        public string Description { get; set; } = string.Empty;

        // Onde ocorreu?
        public int BuildingId { get; set; }
        public Building? Building { get; set; }
    }
}