using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Services
{
    public static class ServiceHelper
    {
        public static IServiceCollection AddEmailTemplate<TTemplateEmailService>(this IServiceCollection services)
            where TTemplateEmailService : class, ITemplateEmailService
        {
            services.AddScoped<ITemplateEmailService, TTemplateEmailService>();
            return services;
        }

        public static IServiceCollection AddTokenService<TTokenService>(this IServiceCollection services)
            where TTokenService : class, ITokenService
        {
            services.AddScoped<ITokenService, TTokenService>();
            return services;
        }

        public static IServiceCollection AddPasswordService<TPasswordService>(this IServiceCollection services)
            where TPasswordService : class, IPasswordHashService
        {
            services.AddScoped<IPasswordHashService, TPasswordService>();
            return services;
        }

        public static IServiceCollection ConfigServices<TTemplateEmailService, TTokenService, TPasswordService>(this IServiceCollection services)
            where TTemplateEmailService : class, ITemplateEmailService
            where TTokenService : class, ITokenService
            where TPasswordService : class, IPasswordHashService
        {
            services.AddEmailTemplate<TTemplateEmailService>();
            services.AddTokenService<TTokenService>();
            services.AddPasswordService<TPasswordService>();
            return services;
        }
    }
}
