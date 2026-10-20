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
        public void GetName_WhenNameIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                version: null!,
                name: "User Management",
                description: null!,
                resourceType: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Equal("User Management", result);
        }

        [Fact]
        public void GetName_WhenNameIsNull_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                version: null!,
                name: null!,
                description: null!,
                resourceType: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WhenNameIsWhitespace_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                version: null!,
                name: "   ",
                description: null!,
                resourceType: null!);

            var result = ServiceAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsSet_ShouldReturnValue()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                version: null!,
                name: null!,
                description: "Endpoints for managing users.",
                resourceType: null!);

            var result = ServiceAttributeHelper.GetDescription(attr);

            Assert.Equal("Endpoints for managing users.", result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsNull_ShouldReturnNull()
        {
            var attr = new ServiceAttribute(
                baseRoute: "api/users",
                version: null!,
                name: null!,
                description: null!,
                resourceType: null!);

            var result = ServiceAttributeHelper.GetDescription(attr);

            Assert.Null(result);
        }
    }
}
