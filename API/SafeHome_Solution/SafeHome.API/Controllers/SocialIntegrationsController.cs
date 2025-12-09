using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHome.API.DTOs;
using SafeHome.API.Services;

namespace SafeHome.API.Controllers
{
    [Route("api/social")]
    [ApiController]
    [Authorize]
    public class SocialIntegrationsController : ControllerBase
    {
        private readonly ISocialIntegrationService _socialIntegrationService;

        public SocialIntegrationsController(ISocialIntegrationService socialIntegrationService)
        {
            _socialIntegrationService = socialIntegrationService;
        }

        [HttpPost("incidents/{id}/share")]
        public async Task<ActionResult<SocialShareResultDto>> ShareIncident(int id, [FromBody] SocialShareRequestDto request)
        {
            try
            {
                var result = await _socialIntegrationService.ShareIncidentAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
