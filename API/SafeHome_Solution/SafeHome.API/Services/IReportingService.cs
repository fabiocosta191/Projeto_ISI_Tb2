using System.Threading.Tasks;
using SafeHome.API.DTOs;

namespace SafeHome.API.Services
{
    public interface IReportingService
    {
        Task<DashboardSnapshotDto> GetDashboardAsync();
        Task<string> ExportAlertsCsvAsync();
        Task<string> ExportIncidentsCsvAsync();
    }
}
