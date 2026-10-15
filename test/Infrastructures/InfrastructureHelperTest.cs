using BlackDigital.AspNet.Infrastructures;
using BlackDigital.Domain.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlackDigital.AspNet.Test.Infrastructures
{
    public class InfrastructureHelperTest
    {
        private sealed class StubEmailService : IEmailService
        {
            public Task SendEmailAsync(Email email) => Task.CompletedTask;
            public Task SendEmailAsync(Func<Email, Email> emailFactory) => Task.CompletedTask;
        }

        [Fact]
        public void AddEmailService_ShouldRegisterScopedService()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddEmailService<StubEmailService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<IEmailService>();
            Assert.NotNull(service);
            Assert.IsType<StubEmailService>(service);
        }

        [Fact]
        public void AddSmtpService_ShouldRegisterSmtpEmailService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            services.AddSmtpService();

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<IEmailService>();
            Assert.NotNull(service);
            Assert.IsType<SmtpEmailService>(service);
        }

        [Fact]
        public void AddEmailService_ShouldReturnSameServiceCollectionForChaining()
        {
            var services = new ServiceCollection();

            var result = services.AddEmailService<StubEmailService>();

            Assert.Same(services, result);
        }

        [Fact]
        public void AddSmtpService_ShouldReturnSameServiceCollectionForChaining()
        {
            var services = new ServiceCollection();

            var result = services.AddSmtpService();

            Assert.Same(services, result);
        }
    }
}
