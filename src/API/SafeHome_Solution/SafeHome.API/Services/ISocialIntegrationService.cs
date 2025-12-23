using System.Threading.Tasks;
using SafeHome.API.DTOs;

namespace SafeHome.API.Services
{
    public interface ISocialIntegrationService
    {
        Task<SocialShareResultDto> ShareIncidentAsync(int incidentId, SocialShareRequestDto request);
    }
}
