using BlackDigital.AspNet.Infrastructures;
using BlackDigital.AspNet.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlackDigital.AspNet.Services
{
    public static class ServiceHelper
    {
        // ---------------------------------------------------------------
        // Generic registrations (caller supplies the implementation type)
        // ---------------------------------------------------------------

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

        // ---------------------------------------------------------------
        // Convenience registrations for the built-in implementations
        // ---------------------------------------------------------------

        /// <summary>
        /// Registers the built-in <see cref="HtmlTemplateEmailService"/> as the
        /// <see cref="ITemplateEmailService"/> implementation. Requires an
        /// <see cref="IEmailService"/> to be registered (e.g. via
        /// <c>AddSmtpService</c>).
        /// </summary>
        public static IServiceCollection AddHtmlTemplateEmailService(this IServiceCollection services)
            => services.AddEmailTemplate<HtmlTemplateEmailService>();

        /// <summary>
        /// Registers the built-in <see cref="Argon2idPasswordHashService"/> as
        /// the <see cref="IPasswordHashService"/> implementation with default
        /// Argon2id parameters.
        /// </summary>
        public static IServiceCollection AddArgon2idPasswordHashService(this IServiceCollection services)
            => services.AddPasswordService<Argon2idPasswordHashService>();

        /// <summary>
        /// Registers the built-in <see cref="Argon2idPasswordHashService"/> with
        /// custom Argon2id parameters.
        /// </summary>
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
        /// Registers the built-in <see cref="JwtService"/> as the
        /// <see cref="ITokenService"/> implementation, reading JWT settings from
        /// the <c>"Jwt"</c> configuration section (keys: <c>SecretKey</c>,
        /// <c>Issuer</c>, <c>Audience</c>, <c>ExpirationMinutes</c>).
        /// </summary>
        public static IServiceCollection AddJwtService(this IServiceCollection services, IConfiguration configuration)
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
                var logger = sp.GetService<ILogger<JwtService>>();
                return new JwtService(secretKey, issuer, audience, expirationMinutes, logger);
            });
            return services;
        }

        /// <summary>
        /// Registers the built-in <see cref="JwtService"/> with explicit JWT
        /// settings.
        /// </summary>
        public static IServiceCollection AddJwtService(
            this IServiceCollection services,
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes = 1440)
        {
            services.AddSingleton<ITokenService>(_ => new JwtService(secretKey, issuer, audience, expirationMinutes));
            return services;
        }
    }
}
