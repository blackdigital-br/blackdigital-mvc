using BlackDigital.AspNet.OpenApi;

namespace BlackDigital.AspNet.Test.OpenApi
{
    public class ActionAttributeHelperTest
    {
        /// <summary>
        /// A minimal stub that exposes the same property/field names the
        /// reflection-based <see cref="ActionAttributeHelper"/> inspects.
        /// </summary>
        private class StubActionAttribute
        {
            public string? Route { get; set; }
            public HttpMethod? Method { get; set; }
            public bool Authorize { get; set; }
            public string? Policy { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }

            public string? GetName() => Name;
            public string? GetDescription() => Description;
        }

        [Fact]
        public void GetRoute_WhenRouteIsSet_ShouldReturnValue()
        {
            var attr = new StubActionAttribute { Route = "users" };

            var result = ActionAttributeHelper.GetRoute(attr);

            Assert.Equal("users", result);
        }

        [Fact]
        public void GetRoute_WhenRouteIsNull_ShouldReturnEmpty()
        {
            var attr = new StubActionAttribute { Route = null };

            var result = ActionAttributeHelper.GetRoute(attr);

            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("Get")]
        [InlineData("Post")]
        [InlineData("Put")]
        [InlineData("Delete")]
        [InlineData("Patch")]
        public void GetOperationType_ShouldReturnMatchingMethod(string methodName)
        {
            var method = typeof(HttpMethod).GetProperty(methodName)!.GetValue(null) as HttpMethod;
            var attr = new StubActionAttribute { Method = method! };

            var result = ActionAttributeHelper.GetOperationType(attr);

            Assert.Equal(methodName.ToLowerInvariant(), result.ToString()!.ToLowerInvariant());
        }

        [Fact]
        public void GetAuthorize_WhenTrue_ShouldReturnTrue()
        {
            var attr = new StubActionAttribute { Authorize = true };

            var result = ActionAttributeHelper.GetAuthorize(attr);

            Assert.True(result);
        }

        [Fact]
        public void GetAuthorize_WhenFalse_ShouldReturnFalse()
        {
            var attr = new StubActionAttribute { Authorize = false };

            var result = ActionAttributeHelper.GetAuthorize(attr);

            Assert.False(result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsSet_ShouldReturnValue()
        {
            var attr = new StubActionAttribute { Policy = "Category.Read" };

            var result = ActionAttributeHelper.GetPolicy(attr);

            Assert.Equal("Category.Read", result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsNull_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Policy = null };

            var result = ActionAttributeHelper.GetPolicy(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetPolicy_WhenPolicyIsWhitespace_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Policy = "   " };

            var result = ActionAttributeHelper.GetPolicy(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WhenNameIsSet_ShouldReturnValue()
        {
            var attr = new StubActionAttribute { Name = "List Users" };

            var result = ActionAttributeHelper.GetName(attr);

            Assert.Equal("List Users", result);
        }

        [Fact]
        public void GetName_WhenNameIsNull_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Name = null };

            var result = ActionAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WhenNameIsWhitespace_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Name = "   " };

            var result = ActionAttributeHelper.GetName(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsSet_ShouldReturnValue()
        {
            var attr = new StubActionAttribute { Description = "Returns all users." };

            var result = ActionAttributeHelper.GetDescription(attr);

            Assert.Equal("Returns all users.", result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsNull_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Description = null };

            var result = ActionAttributeHelper.GetDescription(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetDescription_WhenDescriptionIsWhitespace_ShouldReturnNull()
        {
            var attr = new StubActionAttribute { Description = "   " };

            var result = ActionAttributeHelper.GetDescription(attr);

            Assert.Null(result);
        }

        [Fact]
        public void GetName_WithRealActionAttribute_ShouldReturnValue()
        {
            var attr = new BlackDigital.Rest.ActionAttribute(
                route: "users",
                method: BlackDigital.Rest.RestMethod.Get,
                authorize: false,
                returnIsSuccess: false,
                keyIdentifier: null!,
                version: null!,
                policy: null!,
                name: "Get Users",
                description: null!,
                resourceType: null!);

            var result = ActionAttributeHelper.GetName(attr);

            Assert.Equal("Get Users", result);
        }

        [Fact]
        public void GetDescription_WithRealActionAttribute_ShouldReturnValue()
        {
            var attr = new BlackDigital.Rest.ActionAttribute(
                route: "users",
                method: BlackDigital.Rest.RestMethod.Get,
                authorize: false,
                returnIsSuccess: false,
                keyIdentifier: null!,
                version: null!,
                policy: null!,
                name: null!,
                description: "Fetches the user list.",
                resourceType: null!);

            var result = ActionAttributeHelper.GetDescription(attr);

            Assert.Equal("Fetches the user list.", result);
        }
    }
}
