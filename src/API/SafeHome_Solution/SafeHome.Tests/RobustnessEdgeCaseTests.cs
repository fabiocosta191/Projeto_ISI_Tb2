using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SafeHome.API.Controllers;
using SafeHome.API.Services;
using Xunit;

namespace SafeHome.Tests
{
    public class RobustnessEdgeCaseTests
    {
        private static IConfiguration CreateJwtConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "testkeytestkeytestkeytestkey" },
                    { "Jwt:Issuer", "issuer" },
                    { "Jwt:Audience", "aud" },
                })
                .Build();
        }

        [Fact]
        public async Task BuildingService_DeleteBuilding_ReturnsFalse_WhenBuildingDoesNotExist()
        {
            await using var db = await TestDatabase.CreateAsync();
            var service = new BuildingService(db.ConnectionFactory);

            var deleted = await service.DeleteBuilding(999);

            Assert.False(deleted);
        }

        [Fact]
        public async Task SensorService_DeleteSensor_ReturnsFalse_WhenSensorDoesNotExist()
        {
            await using var db = await TestDatabase.CreateAsync();
            var service = new SensorService(db.ConnectionFactory);

            var deleted = await service.DeleteSensor(999);

            Assert.False(deleted);
        }

        [Fact]
        public async Task AuthController_Me_ReturnsNotFound_WhenUserClaimExistsButUserMissingInDb()
        {
            await using var db = await TestDatabase.CreateAsync();
            var controller = new AuthController(db.ConnectionFactory, CreateJwtConfig());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "ghost-user") }, "Test"))
                }
            };

            var response = await controller.GetCurrentUser();

            Assert.True(
                response.Result is NotFoundResult ||
                response.Result is NotFoundObjectResult
            );
        }
    }
}
