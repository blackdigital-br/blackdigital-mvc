using BlackDigital.AspNet.Security;
using BlackDigital.Domain.Infrastructures;
using BlackDigital.Domain.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlackDigital.AspNet.Services
{
    /// <summary>
    /// Helper methods for registering application services (e-mail templates,
    /// tokens and password hashing) into the dependency injection container.
    /// </summary>
    public static class ServiceHelper
    {
        // ---------------------------------------------------------------
        // Generic registrations (caller supplies the implementation type)
        // ---------------------------------------------------------------

        /// <summary>
        /// Registers a custom <see cref="ITemplateEmailService"/> implementation
        /// with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TTemplateEmailService">The template e-mail service implementation to register.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEmailTemplate<TTemplateEmailService>(this IServiceCollection services)
            where TTemplateEmailService : class, ITemplateEmailService
        {
            services.AddScoped<ITemplateEmailService, TTemplateEmailService>();
            return services;
        }

        /// <summary>
        /// Registers a custom <see cref="ITokenService"/> implementation with a
        /// scoped lifetime.
        /// </summary>
        /// <typeparam name="TTokenService">The token service implementation to register.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTokenService<TTokenService>(this IServiceCollection services)
            where TTokenService : class, ITokenService
        {
            services.AddScoped<ITokenService, TTokenService>();
            return services;
        }

        /// <summary>
        /// Registers a custom <see cref="IPasswordHashService"/> implementation
        /// with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TPasswordService">The password hash service implementation to register.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPasswordService<TPasswordService>(this IServiceCollection services)
            where TPasswordService : class, IPasswordHashService
        {
            services.AddScoped<IPasswordHashService, TPasswordService>();
            return services;
        }

        /// <summary>
        /// Registers custom implementations for the template e-mail, token and
        /// password hash services in a single call.
        /// </summary>
        /// <typeparam name="TTemplateEmailService">The template e-mail service implementation to register.</typeparam>
        /// <typeparam name="TTokenService">The token service implementation to register.</typeparam>
        /// <typeparam name="TPasswordService">The password hash service implementation to register.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
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

        // ---------------------------------------------------------------
        // Convenience registrations for the built-in implementations
        // ---------------------------------------------------------------

        /// <summary>
        /// Registers the built-in <see cref="HtmlTemplateEmailService"/> as the
        /// <see cref="ITemplateEmailService"/> implementation. Requires an
        /// <see cref="IEmailService"/> to be registered (e.g. via
        /// <c>AddSmtpService</c>).
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHtmlTemplateEmailService(this IServiceCollection services)
            => services.AddEmailTemplate<HtmlTemplateEmailService>();

        /// <summary>
        /// Registers the built-in <see cref="Argon2idPasswordHashService"/> as
        /// the <see cref="IPasswordHashService"/> implementation with default
        /// Argon2id parameters.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddArgon2idPasswordHashService(this IServiceCollection services)
            => services.AddPasswordService<Argon2idPasswordHashService>();

        /// <summary>
        /// Registers the built-in <see cref="Argon2idPasswordHashService"/> with
        /// custom Argon2id parameters.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="memorySize">The Argon2id memory size in KB.</param>
        /// <param name="iterations">The number of Argon2id iterations.</param>
        /// <param name="parallelism">The degree of parallelism.</param>
        /// <param name="saltLength">The salt length in bytes.</param>
        /// <param name="hashLength">The hash length in bytes.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddArgon2idPasswordHashService(
            this IServiceCollection services,
            int memorySize = 65536,
            int iterations = 3,
            int parallelism = 1,
            int saltLength = 16,
            int hashLength = 32)
        {
            services.AddSingleton<IPasswordHashService>(_ =>
                new Argon2idPasswordHashService(memorySize, iterations, parallelism, saltLength, hashLength));
            return services;
        }

        /// <summary>
        /// Registers the built-in <see cref="JwtTokenService"/> as the
        /// <see cref="ITokenService"/> implementation, reading JWT settings from
        /// the <c>"Jwt"</c> configuration section (keys: <c>SecretKey</c>,
        /// <c>Issuer</c>, <c>Audience</c>, <c>ExpirationMinutes</c>).
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a required JWT setting (<c>SecretKey</c>, <c>Issuer</c> or <c>Audience</c>) is not configured.</exception>
        public static IServiceCollection AddJwtTokenService(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("Jwt");
            var secretKey = section["SecretKey"] ?? configuration["Jwt:SecretKey"];
            var issuer = section["Issuer"] ?? configuration["Jwt:Issuer"];
            var audience = section["Audience"] ?? configuration["Jwt:Audience"];
            var expirationMinutes = int.TryParse(section["ExpirationMinutes"], out var exp) ? exp : 1440;

            // Fail fast at registration time if required JWT settings are missing,
            // instead of deferring the NullReferenceException to first token use.
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("JWT 'SecretKey' is not configured.");
            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("JWT 'Issuer' is not configured.");
            if (string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("JWT 'Audience' is not configured.");

            services.AddSingleton<ITokenService>(sp =>
            {
                var logger = sp.GetService<ILogger<JwtTokenService>>();
                return new JwtTokenService(secretKey, issuer, audience, expirationMinutes, logger);
            });
            return services;
        }

        /// <summary>
        /// Registers the built-in <see cref="JwtTokenService"/> with explicit JWT
        /// settings.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="secretKey">The secret key used to sign tokens.</param>
        /// <param name="issuer">The token issuer claim value.</param>
        /// <param name="audience">The token audience claim value.</param>
        /// <param name="expirationMinutes">The token expiration in minutes. Defaults to 1440.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddJwtTokenService(
            this IServiceCollection services,
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes = 1440)
        {
            services.AddSingleton<ITokenService>(_ => new JwtTokenService(secretKey, issuer, audience, expirationMinutes));
            return services;
        }
    }
}
