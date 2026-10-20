using BlackDigital.AspNet.Rest;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Authorization;
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
using AuthorizeAttribute = BlackDigital.Domain.Security.AuthorizeAttribute;

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

        private static RestRouteEntry CreateRouteEntry(string pattern)
        {
            var service = ServiceDescriptor.Singleton(typeof(object), new object());
            var method = typeof(object).GetMethod(nameof(object.ToString))!;
            return new RestRouteEntry(pattern, service, method);
        }

        [Fact]
        public void MatchesPattern_WhenExactMatch_ShouldReturnTrue()
        {
            // Arrange
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Static);
            var routeValues = new Dictionary<string, object>();
            var pathSegments = "/api/test".Split('/', StringSplitOptions.RemoveEmptyEntries);
            var entry = CreateRouteEntry("/api/test");

            // Act
            var result = matchesPatternMethod?.Invoke(null, new object[] { pathSegments, entry, routeValues });

            // Assert
            Assert.True((bool)result!);
        }

        [Fact]
        public void MatchesPattern_WhenDifferentSegmentCount_ShouldReturnFalse()
        {
            // Arrange
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Static);
            var routeValues = new Dictionary<string, object>();
            var pathSegments = "/api/test".Split('/', StringSplitOptions.RemoveEmptyEntries);
            var entry = CreateRouteEntry("/api/test/extra");

            // Act
            var result = matchesPatternMethod?.Invoke(null, new object[] { pathSegments, entry, routeValues });

            // Assert
            Assert.False((bool)result!);
        }

        [Fact]
        public void MatchesPattern_WhenParameterInRoute_ShouldExtractParameter()
        {
            // Arrange
            var matchesPatternMethod = typeof(RestMiddleware)
                .GetMethod("MatchesPattern", BindingFlags.NonPublic | BindingFlags.Static);
            var routeValues = new Dictionary<string, object>();
            var pathSegments = "/api/test/123".Split('/', StringSplitOptions.RemoveEmptyEntries);
            var entry = CreateRouteEntry("/api/test/{id}");

            // Act
            var result = matchesPatternMethod?.Invoke(null, new object[] { pathSegments, entry, routeValues });

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

        // --- Authorization ([Authorize] on the implementation) ---

        public interface IAuthService
        {
            Task<string> GetPublicAsync();
            Task<string> GetAuthenticatedAsync();
            Task<string> GetAdminAsync();
            Task<string> GetPolicyAsync();
        }

        public class AuthService : IAuthService
        {
            public Task<string> GetPublicAsync() => Task.FromResult("public");

            [Authorize]
            public Task<string> GetAuthenticatedAsync() => Task.FromResult("authenticated");

            [Authorize(Roles = new[] { "Admin" })]
            public Task<string> GetAdminAsync() => Task.FromResult("admin");

            [Authorize(Policy = "Read")]
            public Task<string> GetPolicyAsync() => Task.FromResult("policy");
        }

        public interface IClassAuthService
        {
            Task<string> GetAsync();
            Task<string> GetAdminAsync();
        }

        // Class-level requirement applies to every action; the method-level one
        // is additional and combined with OR semantics.
        [Authorize(Roles = new[] { "Manager" })]
        public class ClassAuthService : IClassAuthService
        {
            public Task<string> GetAsync() => Task.FromResult("value");

            [Authorize(Roles = new[] { "Admin" })]
            public Task<string> GetAdminAsync() => Task.FromResult("admin");
        }

        private static ClaimsPrincipal CreateUser(params string[] roles)
        {
            var claims = roles.Select(role => new Claim(ClaimTypes.Role, role));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        private static DefaultHttpContext CreateAuthContext(ClaimsPrincipal? user = null, IServiceProvider? services = null)
        {
            var context = new DefaultHttpContext
            {
                RequestServices = services ?? new ServiceCollection().BuildServiceProvider(),
                User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
            };
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static Task<bool> InvokeIsAuthorizedAsync(RestMiddleware middleware, HttpContext context,
                                                          Type implementationType, MethodInfo methodInfo)
        {
            var method = typeof(RestMiddleware)
                .GetMethod("IsAuthorizedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

            return (Task<bool>)method.Invoke(middleware, new object[] { context, implementationType, methodInfo })!;
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenNoRequirements_ShouldReturnTrue()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext();
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetPublicAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenUnauthenticated_ShouldReturnFalseAndWrite401()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext();
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetAuthenticatedAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.False(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenAuthenticated_ShouldReturnTrue()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext(CreateUser());
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetAuthenticatedAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenAuthenticatedRoleMatches_ShouldReturnTrue()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext(CreateUser("Admin"));
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetAdminAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenAuthenticatedRoleDoesNotMatch_ShouldReturnFalseAndWrite403()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext(CreateUser("User"));
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetAdminAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.False(result);
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenAnyRequirementSatisfied_ShouldReturnTrue()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            // Class requires "Manager", method requires "Admin"; holding only
            // "Admin" satisfies the method requirement (OR semantics).
            var context = CreateAuthContext(CreateUser("Admin"));
            var methodInfo = typeof(IClassAuthService).GetMethod(nameof(IClassAuthService.GetAdminAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(ClassAuthService), methodInfo);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenPolicySatisfied_ShouldReturnTrue()
        {
            // Arrange
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(AuthorizationResult.Success());
            var services = new ServiceCollection();
            services.AddSingleton(authorizationService.Object);
            var context = CreateAuthContext(CreateUser(), services.BuildServiceProvider());

            var middleware = CreateMiddleware(new ServiceCollection());
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetPolicyAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenPolicyNotSatisfied_ShouldReturnFalseAndWrite403()
        {
            // Arrange
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(AuthorizationResult.Failed());
            var services = new ServiceCollection();
            services.AddSingleton(authorizationService.Object);
            var context = CreateAuthContext(CreateUser(), services.BuildServiceProvider());

            var middleware = CreateMiddleware(new ServiceCollection());
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetPolicyAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.False(result);
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        [Fact]
        public async Task IsAuthorizedAsync_WhenPolicyDeclaredButNoAuthorizationService_ShouldReturnFalse()
        {
            // Arrange
            var middleware = CreateMiddleware(new ServiceCollection());
            var context = CreateAuthContext(CreateUser());
            var methodInfo = typeof(IAuthService).GetMethod(nameof(IAuthService.GetPolicyAsync))!;

            // Act
            var result = await InvokeIsAuthorizedAsync(middleware, context, typeof(AuthService), methodInfo);

            // Assert
            Assert.False(result);
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

            var values = new object?[1];

            // Act
            await (Task)prepareMethod!
                .Invoke(middleware, new object[] { "GET:api/test", _mockHttpContext.Object, method, values, new Dictionary<string, object>() })!;

            // Assert
            Assert.Equal(aborted, values[0]);
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

            var values = new object?[1];

            // Act
            await (Task)prepareMethod!
                .Invoke(middleware, new object[] { "POST:api/upload", _mockHttpContext.Object, method, values, new Dictionary<string, object>() })!;

            // Assert
            Assert.Same(file.Object, values[0]);
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

            var values = new object?[1];

            // Act
            await (Task)prepareMethod!
                .Invoke(middleware, new object[] { "POST:api/upload", _mockHttpContext.Object, method, values, new Dictionary<string, object>() })!;

            // Assert
            var bound = Assert.IsType<FormFileCollection>(values[0]);
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
