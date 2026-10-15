using BlackDigital.AspNet.Services;
using BlackDigital.Domain.Infrastructures;
using BlackDigital.Domain.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;

namespace BlackDigital.AspNet.Test.Services
{
    public class ServiceHelperTest
    {
        private sealed class StubTemplateEmailService : ITemplateEmailService
        {
            public Task SendTemplateEmailAsync(TemplateEmail email) => Task.CompletedTask;
            public Task SendTemplateEmailAsync(Func<TemplateEmail, TemplateEmail> emailFactory) => Task.CompletedTask;
        }

        private sealed class StubTokenService : ITokenService
        {
            public string GenerateToken(Guid userId, IEnumerable<Claim> claims, TimeSpan? expiration = null) => "token";
            public ClaimsPrincipal ValidateToken(string token) => new ClaimsPrincipal();
            public string GenerateRefreshToken() => "refresh";
            public Guid? GetTokenIdFromJwt(string token) => Guid.NewGuid();
            public bool IsTokenExpired(string token) => false;
            public DateTime GetTokenExpiration(string token) => DateTime.UtcNow;
        }

        private sealed class StubPasswordService : IPasswordHashService
        {
            public string HashPassword(string password) => password;
            public bool VerifyPassword(string password, string hashedPassword) => password == hashedPassword;
        }

        private sealed class StubEmailService : IEmailService
        {
            public Task SendEmailAsync(Email email) => Task.CompletedTask;
            public Task SendEmailAsync(Func<Email, Email> emailFactory) => Task.CompletedTask;
        }

        [Fact]
        public void AddEmailTemplate_ShouldRegisterScopedService()
        {
            var services = new ServiceCollection();

            services.AddEmailTemplate<StubTemplateEmailService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITemplateEmailService>();
            Assert.NotNull(service);
            Assert.IsType<StubTemplateEmailService>(service);
        }

        [Fact]
        public void AddTokenService_ShouldRegisterScopedService()
        {
            var services = new ServiceCollection();

            services.AddTokenService<StubTokenService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITokenService>();
            Assert.NotNull(service);
            Assert.IsType<StubTokenService>(service);
        }

        [Fact]
        public void AddPasswordService_ShouldRegisterScopedService()
        {
            var services = new ServiceCollection();

            services.AddPasswordService<StubPasswordService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<IPasswordHashService>();
            Assert.NotNull(service);
            Assert.IsType<StubPasswordService>(service);
        }

        [Fact]
        public void ConfigServices_ShouldRegisterAllThreeServices()
        {
            var services = new ServiceCollection();

            services.ConfigServices<StubTemplateEmailService, StubTokenService, StubPasswordService>();

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<ITemplateEmailService>());
            Assert.NotNull(provider.GetService<ITokenService>());
            Assert.NotNull(provider.GetService<IPasswordHashService>());
        }

        [Fact]
        public void AddArgon2idPasswordHashService_Default_ShouldRegisterService()
        {
            var services = new ServiceCollection();

            services.AddArgon2idPasswordHashService();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<IPasswordHashService>();
            Assert.NotNull(service);
            Assert.IsType<BlackDigital.AspNet.Security.Argon2idPasswordHashService>(service);
        }

        [Fact]
        public void AddArgon2idPasswordHashService_WithCustomParams_ShouldRegisterService()
        {
            var services = new ServiceCollection();

            services.AddArgon2idPasswordHashService(
                memorySize: 1024,
                iterations: 1,
                parallelism: 1,
                saltLength: 8,
                hashLength: 16);

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<IPasswordHashService>();
            Assert.NotNull(service);
            var hash = service.HashPassword("test");
            Assert.Contains("m=1024,t=1,p=1", hash);
        }

        [Fact]
        public void AddJwtTokenService_WithConfiguration_ShouldRegisterService()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "this-is-a-secret-key-with-at-least-32-bytes!",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:ExpirationMinutes"] = "60"
                })
                .Build();

            services.AddJwtTokenService(config);

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITokenService>();
            Assert.NotNull(service);
        }

        [Theory]
        [InlineData("Jwt:SecretKey")]
        [InlineData("Jwt:Issuer")]
        [InlineData("Jwt:Audience")]
        public void AddJwtTokenService_WhenRequiredSettingMissing_ShouldThrow(string missingKey)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "this-is-a-secret-key-with-at-least-32-bytes!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            };
            settings.Remove(missingKey);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => services.AddJwtTokenService(config));
        }

        [Fact]
        public void AddJwtTokenService_WithExplicitParams_ShouldRegisterService()
        {
            var services = new ServiceCollection();

            services.AddJwtTokenService(
                secretKey: "this-is-a-secret-key-with-at-least-32-bytes!",
                issuer: "test-issuer",
                audience: "test-audience",
                expirationMinutes: 30);

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITokenService>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddHtmlTemplateEmailService_ShouldRegisterService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton(Mock.Of<IHostEnvironment>());
            // HtmlTemplateEmailService requires IEmailService; register a stub.
            services.AddScoped<IEmailService, StubEmailService>();

            services.AddHtmlTemplateEmailService();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITemplateEmailService>();
            Assert.NotNull(service);
        }
    }
}
