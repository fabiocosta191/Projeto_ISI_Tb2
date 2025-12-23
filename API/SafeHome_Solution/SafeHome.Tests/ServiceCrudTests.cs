using SafeHome.API.Services;
using SafeHome.API.Soap;
using SafeHome.Data;
using SafeHome.Data.Models;
using Xunit;

namespace SafeHome.Tests
{
    public class ServiceCrudTests
    {
        private static AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        [Fact]
        public async Task IncidentService_CreatesAndUpdates()
        {
            using var context = CreateContext();
            var service = new IncidentService(context);

            var created = await service.CreateIncident(new Incident
            {
                Type = "Fire",
                Description = "Smoke detected",
                BuildingId = 1,
                Severity = "High",
                Status = "Reported"
            });

            Assert.True(created.Id > 0);

            created.Status = "Resolved";
            var updated = await service.UpdateIncident(created.Id, created);

            Assert.True(updated);
            var fetched = await service.GetIncidentById(created.Id);
            Assert.Equal("Resolved", fetched?.Status);
        }

        [Fact]
        public async Task AlertService_StoresAndDeletes()
        {
            using var context = CreateContext();
            var service = new AlertService(context);

            var created = await service.CreateAsync(new Alert
            {
                Message = "Temperature high",
                SensorId = 2,
                Severity = "Critical"
            });

            Assert.NotEqual(0, created.Id);

            var deleted = await service.DeleteAsync(created.Id);
            Assert.True(deleted);
            Assert.Empty(await service.GetAllAsync());
        }

        [Fact]
        public async Task UserService_HashesPasswords()
        {
            using var context = CreateContext();
            var service = new UserService(context);

            var user = await service.CreateAsync("alice", "pass123", "Admin");

            Assert.NotEqual("pass123", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("pass123", user.PasswordHash));

            var roleUpdated = await service.UpdateRoleAsync(user.Id, "User");
            Assert.True(roleUpdated);
            Assert.Equal("User", (await service.GetByIdAsync(user.Id))?.Role);
        }
    }
}
