
using BlackDigital.Domain.Infrastructures;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Infrastructures
{
    /// <summary>
    /// Helper methods for registering infrastructure services (e-mail) into the
    /// dependency injection container.
    /// </summary>
    public static class InfrastructureHelper
    {
        /// <summary>
        /// Registers a custom <see cref="IEmailService"/> implementation with a
        /// scoped lifetime.
        /// </summary>
        /// <typeparam name="TEmailService">The e-mail service implementation to register.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEmailService<TEmailService>(this IServiceCollection services)
            where TEmailService : class, IEmailService
        {
            services.AddScoped<IEmailService, TEmailService>();
            return services;
        }

        /// <summary>
        /// Registers the built-in <see cref="SmtpEmailService"/> as the
        /// <see cref="IEmailService"/> implementation with a scoped lifetime.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSmtpService(this IServiceCollection services)
            => services.AddEmailService<SmtpEmailService>();
    }
}
