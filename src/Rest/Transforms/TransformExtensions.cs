using Microsoft.Extensions.DependencyInjection;

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
            var builder = new TransformBuilder();
            builder = configBuilder(builder);
            services.AddSingleton(builder);
            services.AddScoped<TransformManager>();

            return services;
        }
    }
}
