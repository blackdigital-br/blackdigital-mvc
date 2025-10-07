
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Infrastructures
{
    public static class InfrastructureHelper
    {
        public static IServiceCollection AddEmailService<TEmailService>(this IServiceCollection services)
            where TEmailService : class, IEmailService
        {
            services.AddScoped<IEmailService, TEmailService>();
            return services;
        }

        public static IServiceCollection AddSmtpService(this IServiceCollection services)
            => services.AddEmailService<SmtpEmailService>();
    }
}
