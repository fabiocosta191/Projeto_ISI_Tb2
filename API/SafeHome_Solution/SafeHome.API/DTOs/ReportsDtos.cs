using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SafeHome.API.DTOs
{
    public class DashboardSnapshotDto
    {
        public int TotalBuildings { get; set; }
        public int TotalSensors { get; set; }
        public int ActiveSensors { get; set; }
        public int InactiveSensors { get; set; }
        public int OpenIncidents { get; set; }
        public int ResolvedIncidents { get; set; }
        public int OpenAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public List<BuildingLoadDto> Buildings { get; set; } = new();
    }

    public class BuildingLoadDto
    {
        public int BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public int SensorCount { get; set; }
        public int OpenIncidents { get; set; }
    }

    public class SensorReadingImportDto
    {
        [Required]
        public int SensorId { get; set; }

        [Required]
        public double? Value { get; set; }

        public DateTime? Timestamp { get; set; }
    }

    public class ImportSummaryDto
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<string> Notes { get; set; } = new();
    }

    public class SocialShareRequestDto
    {
        [Required]
        public string Network { get; set; } = "";

        [StringLength(240)]
        public string? Message { get; set; }
    }

    public class SocialShareResultDto
    {
        public string Network { get; set; } = string.Empty;
        public string PayloadPreview { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }
}
