using BlackDigital.AspNet.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;
using System.Security.Claims;

namespace BlackDigital.AspNet.Test.Services
{
    public class BdHttpContextHelperTest
    {
        [Fact]
        public void GetCurrentIpAddress_WhenRemoteIpAddressIsSet_ShouldReturnIt()
        {
            var httpContext = new Mock<HttpContext>();
            var connection = new Mock<ConnectionInfo>();
            connection.Setup(c => c.RemoteIpAddress).Returns(IPAddress.Parse("203.0.113.5"));
            httpContext.Setup(c => c.Connection).Returns(connection.Object);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentIpAddress();

            Assert.Equal("203.0.113.5", result);
        }

        [Fact]
        public void GetCurrentIpAddress_WhenRemoteIpIsNullAndForwardedForIsSet_ShouldReturnFirstIp()
        {
            var headers = new HeaderDictionary
            {
                ["X-Forwarded-For"] = "198.51.100.10, 10.0.0.1"
            };
            var httpContext = CreateHttpContextWithHeaders(headers, remoteIp: null);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentIpAddress();

            Assert.Equal("198.51.100.10", result);
        }

        [Fact]
        public void GetCurrentIpAddress_WhenRemoteIpAndForwardedForNullAndRealIpSet_ShouldReturnRealIp()
        {
            var headers = new HeaderDictionary
            {
                ["X-Real-IP"] = "192.0.2.99"
            };
            var httpContext = CreateHttpContextWithHeaders(headers, remoteIp: null);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentIpAddress();

            Assert.Equal("192.0.2.99", result);
        }

        [Fact]
        public void GetCurrentIpAddress_WhenNoIpAvailable_ShouldReturnNull()
        {
            var headers = new HeaderDictionary();
            var httpContext = CreateHttpContextWithHeaders(headers, remoteIp: null);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentIpAddress();

            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentIpAddress_WhenHttpContextIsNull_ShouldThrow()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            Assert.Throws<InvalidOperationException>(() => accessor.Object.GetCurrentIpAddress());
        }

        [Fact]
        public void GetCurrentTokenIdentifier_WhenJtiClaimExists_ShouldReturnValue()
        {
            var claims = new List<Claim> { new("jti", "token-123") };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentTokenIdentifier();

            Assert.Equal("token-123", result);
        }

        [Fact]
        public void GetCurrentTokenIdentifier_WhenNoJtiClaim_ShouldReturnNull()
        {
            var claims = new List<Claim> { new("sub", "user-1") };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

            var result = accessor.Object.GetCurrentTokenIdentifier();

            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentTokenIdentifier_WhenHttpContextIsNull_ShouldReturnNull()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var result = accessor.Object.GetCurrentTokenIdentifier();

            Assert.Null(result);
        }

        private static Mock<HttpContext> CreateHttpContextWithHeaders(IHeaderDictionary headers, IPAddress? remoteIp)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers).Returns(headers);

            var connection = new Mock<ConnectionInfo>();
            connection.Setup(c => c.RemoteIpAddress).Returns(remoteIp!);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request).Returns(request.Object);
            httpContext.Setup(c => c.Connection).Returns(connection.Object);

            return httpContext;
        }
    }
}
