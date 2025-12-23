using System.Collections.Generic;
using System.Threading.Tasks;
using SafeHome.API.DTOs;

namespace SafeHome.API.Services
{
    public interface IDataPortabilityService
    {
        Task<string> ExportSensorReadingsCsvAsync(int? sensorId = null);
        Task<ImportSummaryDto> ImportSensorReadingsAsync(IEnumerable<SensorReadingImportDto> readings);
    }
}
