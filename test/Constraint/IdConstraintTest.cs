using BlackDigital.Mvc.Constraint;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace BlackDigital.Mvc.Test.Constraint
{
    public class IdConstraintTest
    {
        private readonly IdConstraint _constraint;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IRouter> _mockRouter;

        public IdConstraintTest()
        {
            _constraint = new IdConstraint();
            _mockHttpContext = new Mock<HttpContext>();
            _mockRouter = new Mock<IRouter>();
        }

        [Fact]
        public void Match_WhenRouteKeyDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var routeValues = new RouteValueDictionary();
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_WhenValueIsNotString_ShouldReturnFalse()
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", 123 } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_WhenValueIsValidString_ShouldReturnTrue()
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", "valid-id-123" } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Match_WhenValueIsEmptyString_ShouldReturnTrue()
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", "" } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Match_WhenValueIsNull_ShouldReturnFalse()
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", null } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_WhenValueIsWhitespaceString_ShouldReturnTrue()
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", "   " } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("123e4567-e89b-12d3-a456-426614174000")]
        [InlineData("simple-id")]
        [InlineData("test123")]
        [InlineData("a")]
        public void Match_WhenValueIsVariousValidStrings_ShouldReturnTrue(string idValue)
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", idValue } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, RouteDirection.IncomingRequest);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(RouteDirection.IncomingRequest)]
        [InlineData(RouteDirection.UrlGeneration)]
        public void Match_ShouldWorkWithDifferentRouteDirections(RouteDirection direction)
        {
            // Arrange
            var routeValues = new RouteValueDictionary { { "id", "test-id" } };
            const string routeKey = "id";

            // Act
            var result = _constraint.Match(_mockHttpContext.Object, _mockRouter.Object, routeKey, routeValues, direction);

            // Assert
            Assert.True(result);
        }
    }
}