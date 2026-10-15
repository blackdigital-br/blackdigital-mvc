using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Extension methods for registering the transform system into the
    /// dependency injection container.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Registers the transform builder and the scoped
        /// <see cref="TransformManager"/> into the service collection.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configBuilder">A delegate that configures the transform rules.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTransform(this IServiceCollection services,
            Func<TransformBuilder, TransformBuilder> configBuilder)
        {
            // Reuse a TransformBuilder already registered by AddRestServices
            // (which scans for [Transform] attributes) so that programmatic and
            // attribute-based rules share the same registry.
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(TransformBuilder))
                ?.ImplementationInstance as TransformBuilder;

            var builder = existing ?? new TransformBuilder();
            builder = configBuilder(builder);

            if (existing == null)
                services.AddSingleton(builder);

            services.TryAddScoped<TransformManager>();

            return services;
        }
    }
}
