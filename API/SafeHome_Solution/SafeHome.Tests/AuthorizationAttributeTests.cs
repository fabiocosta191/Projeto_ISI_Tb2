using System.Linq;
using Microsoft.AspNetCore.Authorization;
using SafeHome.API.Controllers;
using Xunit;

namespace SafeHome.Tests
{
    public class AuthorizationAttributeTests
    {
        [Fact]
        public void ReportsController_HasAuthorizeAttribute()
        {
            var attrs = typeof(ReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
            Assert.NotEmpty(attrs);
        }

        [Fact]
        public void DataPortabilityController_HasAuthorizeAttribute()
        {
            var attrs = typeof(DataPortabilityController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
            Assert.NotEmpty(attrs);
        }

        [Fact]
        public void SocialIntegrationsController_HasAuthorizeAttribute()
        {
            var attrs = typeof(SocialIntegrationsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
            Assert.NotEmpty(attrs);
        }

        [Fact]
        public void AuthController_GetCurrentUser_HasAuthorizeAttribute()
        {
            var method = typeof(AuthController).GetMethod("GetCurrentUser");
            Assert.NotNull(method);

            var attrs = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
            Assert.NotEmpty(attrs);
        }

        [Fact]
        public void AuthController_ChangePassword_HasAuthorizeAttribute()
        {
            var method = typeof(AuthController).GetMethod("ChangePassword");
            Assert.NotNull(method);

            var attrs = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
            Assert.NotEmpty(attrs);
        }
    }
}
