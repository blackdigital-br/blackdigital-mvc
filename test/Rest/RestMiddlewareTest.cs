using BlackDigital.Mvc.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlackDigital.Mvc.Test.Rest
{
    public class RestMiddlewareTest
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<IServiceCollection> _mockServices;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockRequest;
        private readonly Mock<HttpResponse> _mockResponse;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public RestMiddlewareTest()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockServices = new Mock<IServiceCollection>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockRequest = new Mock<HttpRequest>();
            _mockResponse = new Mock<HttpResponse>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            SetupHttpContextMocks();
        }

        private void SetupHttpContextMocks()
        {
            _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
            _mockHttpContext.Setup(x => x.Response).Returns(_mockResponse.Object);
            _mockHttpContext.Setup(x => x.RequestServices).Returns(_mockServiceProvider.Object);

            var responseBodyStream = new MemoryStream();
            _mockResponse.Setup(x => x.Body).Returns(responseBodyStream);
            _mockResponse.SetupProperty(x => x.StatusCode);
            _mockResponse.SetupProperty(x => x.ContentType);

            var user = new ClaimsPrincipal(new ClaimsIdentity());
            _mockHttpContext.Setup(x => x.User).Returns(user);
        }

        [Fact]
        public void Constructor_WhenNextIsNull_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => 
                new RestMiddleware(null!, _mockServices.Object));
        }

        [Fact]
        public void Constructor_WhenServicesIsNull_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => 
                new RestMiddleware(_mockNext.Object, null!));
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var middleware = new RestMiddleware(_mockNext.Object, services);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WhenNoRouteMatches_ShouldCallNextMiddleware()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);

            _mockRequest.Setup(x => x.Path).Returns(new PathString("/unknown"));
            _mockRequest.Setup(x => x.Method).Returns("GET");

            // Act
            await middleware.InvokeAsync(_mockHttpContext.Object);

            // Assert
            _mockNext.Verify(x => x(_mockHttpContext.Object), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenContextIsNull_ShouldThrowNullReferenceException()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => 
                middleware.InvokeAsync(null!));
        }

        [Theory]
        [InlineData("/api/test", "GET")]
        [InlineData("/api/user", "POST")]
        [InlineData("/api/products/123", "DELETE")]
        public async Task InvokeAsync_WhenPathIsValid_ShouldProcessCorrectly(string path, string method)
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);

            _mockRequest.Setup(x => x.Path).Returns(new PathString(path));
            _mockRequest.Setup(x => x.Method).Returns(method);

            // Act
            await middleware.InvokeAsync(_mockHttpContext.Object);

            // Assert
            _mockNext.Verify(x => x(_mockHttpContext.Object), Times.Once);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteIsEmpty_ShouldReturnBaseRoute()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var buildFullRouteMethod = typeof(RestMiddleware)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = buildFullRouteMethod?.Invoke(middleware, new object[] { "/api/test", "" });

            // Assert
            Assert.Equal("/api/test", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteIsNull_ShouldReturnBaseRoute()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var buildFullRouteMethod = typeof(RestMiddleware)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = buildFullRouteMethod?.Invoke(middleware, new object[] { "/api/test", null! });

            // Assert
            Assert.Equal("/api/test", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteStartsWithSlash_ShouldCombineCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var buildFullRouteMethod = typeof(RestMiddleware)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = buildFullRouteMethod?.Invoke(middleware, new object[] { "/api/test", "/action" });

            // Assert
            Assert.Equal("/api/test/action", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteDoesNotStartWithSlash_ShouldAddSlash()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var buildFullRouteMethod = typeof(RestMiddleware)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = buildFullRouteMethod?.Invoke(middleware, new object[] { "/api/test", "action" });

            // Assert
            Assert.Equal("/api/test/action", result);
        }

        [Fact]
        public void MatchesPattern_WhenExactMatch_ShouldReturnTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Instance);
            var routeValues = new Dictionary<string, object>();

            // Act
            var result = matchesPatternMethod?.Invoke(middleware, new object[] { "/api/test", "/api/test", routeValues });

            // Assert
            Assert.True((bool)result!);
        }

        [Fact]
        public void MatchesPattern_WhenDifferentSegmentCount_ShouldReturnFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Instance);
            var routeValues = new Dictionary<string, object>();

            // Act
            var result = matchesPatternMethod?.Invoke(middleware, new object[] { "/api/test", "/api/test/extra", routeValues });

            // Assert
            Assert.False((bool)result!);
        }

        [Fact]
        public void MatchesPattern_WhenParameterInRoute_ShouldExtractParameter()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Instance);
            var routeValues = new Dictionary<string, object>();

            // Act
            var result = matchesPatternMethod?.Invoke(middleware, new object[] { "/api/test/123", "/api/test/{id}", routeValues });

            // Assert
            Assert.True((bool)result!);
            Assert.Equal("123", routeValues["id"]);
        }

        [Fact]
        public void ConvertValue_WhenTargetTypeIsString_ShouldReturnSameValue()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var convertValueMethod = typeof(RestMiddleware)
                .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = convertValueMethod?.Invoke(middleware, new object[] { "test", typeof(string) });

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void ConvertValue_WhenTargetTypeIsInt_ShouldReturnParsedInt()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var convertValueMethod = typeof(RestMiddleware)
                .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = convertValueMethod?.Invoke(middleware, new object[] { "123", typeof(int) });

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public void ConvertValue_WhenTargetTypeIsBool_ShouldReturnParsedBool()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var convertValueMethod = typeof(RestMiddleware)
                .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = convertValueMethod?.Invoke(middleware, new object[] { "true", typeof(bool) });

            // Assert
            Assert.Equal(true, result);
        }

        [Fact]
        public void ConvertValue_WhenTargetTypeIsGuid_ShouldReturnParsedGuid()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var convertValueMethod = typeof(RestMiddleware)
                .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);
            var guidString = Guid.NewGuid().ToString();

            // Act
            var result = convertValueMethod?.Invoke(middleware, new object[] { guidString, typeof(Guid) });

            // Assert
            Assert.Equal(Guid.Parse(guidString), result);
        }

        [Theory]
        [InlineData("123", typeof(int?), 123)]
        [InlineData("true", typeof(bool?), true)]
        [InlineData("456", typeof(long?), 456L)]
        public void ConvertValue_WhenTargetTypeIsNullable_ShouldReturnParsedValue(string input, Type targetType, object expected)
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var convertValueMethod = typeof(RestMiddleware)
                .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = convertValueMethod?.Invoke(middleware, new object[] { input, targetType });

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ReadRequestBody_ShouldReturnBodyContent()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var readRequestBodyMethod = typeof(RestMiddleware)
                .GetMethod("ReadRequestBody", BindingFlags.NonPublic | BindingFlags.Instance);

            var bodyContent = "test body content";
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
            _mockRequest.Setup(x => x.Body).Returns(bodyStream);

            // Act
            var result = await (Task<string>)readRequestBodyMethod?.Invoke(middleware, new object[] { _mockHttpContext.Object })!;

            // Assert
            Assert.Equal(bodyContent, result);
        }

        [Fact]
        public void GetJsonOptions_ShouldReturnDefaultOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = new RestMiddleware(_mockNext.Object, services);
            var getJsonOptionsMethod = typeof(RestMiddleware)
                .GetMethod("getJsonOptions", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = getJsonOptionsMethod?.Invoke(middleware, new object[] { _mockHttpContext.Object });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<JsonSerializerOptions>(result);
        }
    }
}