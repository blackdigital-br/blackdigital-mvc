using BlackDigital.AspNet.OpenApi;
using BlackDigital.Rest;

namespace BlackDigital.AspNet.Test.OpenApi
{
    public class ServiceAttributeHelperTest
    {
        [Fact]
        public void GetRoute_WhenBaseRouteIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute("api/users");

            var result = ServiceAttributeHelper.GetRoute(attr);

            Assert.Equal("api/users", result);
        }

        [Fact]
        public void GetAuthorize_WhenTrue_ShouldReturnTrue()
        {
            var attr = new ServiceAttribute("api/users") { Authorize = true };

            var result = ServiceAttributeHelper.GetAuthorize(attr);

            Assert.True(result);
        }

        [Fact]
        public void GetAuthorize_WhenFalse_ShouldReturnFalse()
        {
            var attr = new ServiceAttribute("api/users") { Authorize = false };

            var result = ServiceAttributeHelper.GetAuthorize(attr);

            Assert.False(result);
        }

        [Fact]
        public void GetAuthorize_WhenNotSet_ShouldReturnFalse()
        {
            var attr = new ServiceAttribute("api/users");

            var result = ServiceAttributeHelper.GetAuthorize(attr);

            Assert.False(result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute("api/users") { Policy = "Category" };

            var result = ServiceAttributeHelper.GetPolicy(attr);

            Assert.Equal("Category", result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsNull_ShouldReturnNull()
        {
            var attr = new ServiceAttribute("api/users");

            var result = ServiceAttributeHelper.GetPolicy(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsWhitespace_ShouldReturnNull()
        {
            var attr = new ServiceAttribute("api/users") { Policy = "   " };

            var result = ServiceAttributeHelper.GetPolicy(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WhenNameIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                authorize: false,
                version: null!,
                name: "User Management",
                description: null!,
                resourceType: null!,
                policy: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Equal("User Management", result);
        }

        [Fact]
        public void GetName_WhenNameIsNull_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                authorize: false,
                version: null!,
                name: null!,
                description: null!,
                resourceType: null!,
                policy: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WhenNameIsWhitespace_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                authorize: false,
                version: null!,
                name: "   ",
                description: null!,
                resourceType: null!,
                policy: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                authorize: false,
                version: null!,
                name: null!,
                description: "Endpoints for managing users.",
                resourceType: null!,
                policy: null!);

            var result = ServiceAttributeHelper.GetDescription(attr);

            Assert.Equal("Endpoints for managing users.", result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsNull_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                authorize: false,
                version: null!,
                name: null!,
                description: null!,
                resourceType: null!,
                policy: null!);

            var result = ServiceAttributeHelper.GetDescription(attr);

            Assert.Null(result);
        }
    }
}
