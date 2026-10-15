using BlackDigital.AspNet.Rest;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlackDigital.AspNet.Test.Rest
{
    public class RestMiddlewareTest
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<IServiceCollection> _mockServices;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockRequest;
        private readonly Mock<HttpResponse> _mockResponse;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<RestMiddleware>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public RestMiddlewareTest()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockServices = new Mock<IServiceCollection>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockRequest = new Mock<HttpRequest>();
            _mockResponse = new Mock<HttpResponse>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<RestMiddleware>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

            SetupHttpContextMocks();
        }

        private RestMiddleware CreateMiddleware(IServiceCollection services)
            => new RestMiddleware(_mockNext.Object, new RestRouteRegistry(services), _mockLogger.Object, _mockEnvironment.Object, Options.Create(new RestOptions()));

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
        public void Constructor_WhenNextIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RestMiddleware(null!, new RestRouteRegistry(new ServiceCollection()), _mockLogger.Object, _mockEnvironment.Object, Options.Create(new RestOptions())));
        }

        [Fact]
        public void Constructor_WhenRouteRegistryIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RestMiddleware(_mockNext.Object, null!, _mockLogger.Object, _mockEnvironment.Object, Options.Create(new RestOptions())));
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var middleware = CreateMiddleware(services);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WhenNoRouteMatches_ShouldCallNextMiddleware()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);

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
            var middleware = CreateMiddleware(services);

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
            var middleware = CreateMiddleware(services);

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
            var buildFullRouteMethod = typeof(RestRouteRegistry)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = buildFullRouteMethod?.Invoke(null, new object[] { "/api/test", "" });

            // Assert
            Assert.Equal("/api/test", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteIsNull_ShouldReturnBaseRoute()
        {
            // Arrange
            var buildFullRouteMethod = typeof(RestRouteRegistry)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = buildFullRouteMethod?.Invoke(null, new object[] { "/api/test", null! });

            // Assert
            Assert.Equal("/api/test", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteStartsWithSlash_ShouldCombineCorrectly()
        {
            // Arrange
            var buildFullRouteMethod = typeof(RestRouteRegistry)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = buildFullRouteMethod?.Invoke(null, new object[] { "/api/test", "/action" });

            // Assert
            Assert.Equal("/api/test/action", result);
        }

        [Fact]
        public void BuildFullRoute_WhenActionRouteDoesNotStartWithSlash_ShouldAddSlash()
        {
            // Arrange
            var buildFullRouteMethod = typeof(RestRouteRegistry)
                .GetMethod("BuildFullRoute", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = buildFullRouteMethod?.Invoke(null, new object[] { "/api/test", "action" });

            // Assert
            Assert.Equal("/api/test/action", result);
        }

        [Fact]
        public void MatchesPattern_WhenExactMatch_ShouldReturnTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
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
            var middleware = CreateMiddleware(services);
            var getJsonOptionsMethod = typeof(RestMiddleware)
                .GetMethod("GetJsonOptions", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = getJsonOptionsMethod?.Invoke(middleware, new object[] { _mockHttpContext.Object });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<JsonSerializerOptions>(result);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", null)]
        [InlineData("  ", "   ", null)]
        [InlineData("Category", null, "Category")]
        [InlineData(null, "Read", "Read")]
        [InlineData("Category", "Read", "Category.Read")]
        [InlineData("Category", "", "Category")]
        [InlineData("", "Read", "Read")]
        public void CombinePolicies_ShouldReturnExpectedValue(string? servicePolicy, string? actionPolicy, string? expected)
        {
            // Arrange
            var combinePoliciesMethod = typeof(RestMiddleware)
                .GetMethod("CombinePolicies", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = combinePoliciesMethod?.Invoke(null, new object?[] { servicePolicy, actionPolicy });

            // Assert
            Assert.Equal(expected, result);
        }

        // --- CancellationToken binding ---

        public interface ICancellationTokenService
        {
            Task<string> GetDataAsync(CancellationToken cancellationToken);
        }

        [Fact]
        public async Task PrepareMethodParameters_WhenParameterIsCancellationToken_ShouldBindRequestAborted()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var method = typeof(ICancellationTokenService).GetMethod(nameof(ICancellationTokenService.GetDataAsync))!;

            var aborted = new CancellationTokenSource().Token;
            _mockHttpContext.Setup(x => x.RequestAborted).Returns(aborted);

            var prepareMethod = typeof(RestMiddleware)
                .GetMethod("PrepareMethodParameters", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = await (Task<object?[]>)prepareMethod!
                .Invoke(middleware, new object[] { "GET:api/test", _mockHttpContext.Object, method, new Dictionary<string, object>() })!;

            // Assert
            Assert.Equal(aborted, result[0]);
        }

        // --- CacheAttribute / ApplyCacheHeaders ---

        [Fact]
        public void ApplyCacheHeaders_WhenPublic_ShouldSetPublicMaxAge()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var cacheAttr = new CacheAttribute(300);

            var headers = new HeaderDictionary();
            _mockResponse.Setup(x => x.Headers).Returns(headers);

            var applyCache = typeof(RestMiddleware)
                .GetMethod("ApplyCacheHeaders", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            applyCache?.Invoke(null, new object[] { _mockHttpContext.Object, cacheAttr });

            // Assert
            Assert.Equal("public, max-age=300", headers["Cache-Control"].ToString());
        }

        [Fact]
        public void ApplyCacheHeaders_WhenClientLocation_ShouldSetPrivateMaxAge()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var cacheAttr = new CacheAttribute(60) { Location = CacheLocation.Client };

            var headers = new HeaderDictionary();
            _mockResponse.Setup(x => x.Headers).Returns(headers);

            var applyCache = typeof(RestMiddleware)
                .GetMethod("ApplyCacheHeaders", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            applyCache?.Invoke(null, new object[] { _mockHttpContext.Object, cacheAttr });

            // Assert
            Assert.Equal("private, max-age=60", headers["Cache-Control"].ToString());
        }

        [Fact]
        public void ApplyCacheHeaders_WhenNoStore_ShouldSetNoStore()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var cacheAttr = new CacheAttribute(0) { NoStore = true };

            var headers = new HeaderDictionary();
            _mockResponse.Setup(x => x.Headers).Returns(headers);

            var applyCache = typeof(RestMiddleware)
                .GetMethod("ApplyCacheHeaders", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            applyCache?.Invoke(null, new object[] { _mockHttpContext.Object, cacheAttr });

            // Assert
            Assert.Equal("no-store", headers["Cache-Control"].ToString());
        }

        // --- RateLimitAttribute / CheckRateLimitAsync ---

        [Fact]
        public async Task CheckRateLimitAsync_WhenUnderLimit_ShouldReturnTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var middleware = CreateMiddleware(services);
            var provider = services.BuildServiceProvider();
            _mockHttpContext.Setup(x => x.RequestServices).Returns(provider);
            _mockHttpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));

            var rateLimit = new RateLimitAttribute(5, 60);
            var checkRateLimit = typeof(RestMiddleware)
                .GetMethod("CheckRateLimitAsync", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = await (Task<bool>)checkRateLimit!
                .Invoke(null, new object[] { _mockHttpContext.Object, "GET:api/test", rateLimit })!;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckRateLimitAsync_WhenExceedsLimit_ShouldReturnFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var middleware = CreateMiddleware(services);
            var provider = services.BuildServiceProvider();
            _mockHttpContext.Setup(x => x.RequestServices).Returns(provider);
            _mockHttpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));

            var rateLimit = new RateLimitAttribute(1, 60);
            var checkRateLimit = typeof(RestMiddleware)
                .GetMethod("CheckRateLimitAsync", BindingFlags.NonPublic | BindingFlags.Static);

            // First call succeeds.
            await (Task<bool>)checkRateLimit!
                .Invoke(null, new object[] { _mockHttpContext.Object, "GET:api/limited", rateLimit })!;

            // Second call should be rejected.
            var result = await (Task<bool>)checkRateLimit
                .Invoke(null, new object[] { _mockHttpContext.Object, "GET:api/limited", rateLimit })!;

            // Assert
            Assert.False(result);
        }

        // --- IAsyncEnumerable streaming ---

        [Fact]
        public void IsAsyncEnumerable_WhenIAsyncEnumerable_ShouldReturnTrueAndElementType()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var isAsyncEnumerable = typeof(RestMiddleware)
                .GetMethod("IsAsyncEnumerable", BindingFlags.NonPublic | BindingFlags.Static);

            async IAsyncEnumerable<int> Generate()
            {
                yield return 1;
                await Task.CompletedTask;
            }

            // Act
            var parameters = new object?[] { Generate(), null, null };
            var result = (bool)isAsyncEnumerable!.Invoke(null, parameters)!;

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(int), parameters[2]);
        }

        [Fact]
        public void IsAsyncEnumerable_WhenNotIAsyncEnumerable_ShouldReturnFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var isAsyncEnumerable = typeof(RestMiddleware)
                .GetMethod("IsAsyncEnumerable", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var parameters = new object?[] { new List<int> { 1, 2, 3 }, null, null };
            var result = (bool)isAsyncEnumerable!.Invoke(null, parameters)!;

            // Assert
            Assert.False(result);
        }

        // --- IFormFile binding ---

        public interface IUploadService
        {
            Task UploadAsync([Body] IFormFile file);
            Task UploadManyAsync([Body] IFormFileCollection files);
        }

        [Fact]
        public async Task PrepareMethodParameters_WhenBodyIsIFormFile_ShouldBindFromFormFiles()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var method = typeof(IUploadService).GetMethod(nameof(IUploadService.UploadAsync))!;

            var file = new Mock<IFormFile>();
            var formFiles = new FormFileCollection { file.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFiles);
            _mockRequest.Setup(x => x.Form).Returns(formCollection);

            var prepareMethod = typeof(RestMiddleware)
                .GetMethod("PrepareMethodParameters", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = await (Task<object?[]>)prepareMethod!
                .Invoke(middleware, new object[] { "POST:api/upload", _mockHttpContext.Object, method, new Dictionary<string, object>() })!;

            // Assert
            Assert.Same(file.Object, result[0]);
        }

        [Fact]
        public async Task PrepareMethodParameters_WhenBodyIsIFormFileCollection_ShouldBindAllFiles()
        {
            // Arrange
            var services = new ServiceCollection();
            var middleware = CreateMiddleware(services);
            var method = typeof(IUploadService).GetMethod(nameof(IUploadService.UploadManyAsync))!;

            var file1 = new Mock<IFormFile>();
            var file2 = new Mock<IFormFile>();
            var formFiles = new FormFileCollection { file1.Object, file2.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFiles);
            _mockRequest.Setup(x => x.Form).Returns(formCollection);

            var prepareMethod = typeof(RestMiddleware)
                .GetMethod("PrepareMethodParameters", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = await (Task<object?[]>)prepareMethod!
                .Invoke(middleware, new object[] { "POST:api/upload", _mockHttpContext.Object, method, new Dictionary<string, object>() })!;

            // Assert
            var bound = Assert.IsType<FormFileCollection>(result[0]);
            Assert.Equal(2, bound.Count);
        }

        // --- RestOptions defaults ---

        [Fact]
        public void RestOptions_Defaults_ShouldHaveLoggingDisabled()
        {
            var options = new RestOptions();
            Assert.False(options.EnableRequestLogging);
            Assert.False(options.LogRequestBody);
            Assert.False(options.LogResponseBody);
        }
    }
}
