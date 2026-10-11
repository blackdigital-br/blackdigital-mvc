using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Extension methods for registering REST services into the dependency
    /// injection container and adding the <see cref="RestMiddleware"/> to the
    /// request pipeline.
    /// </summary>
    public static class RestMiddlewareExtensions
    {
        /// <summary>
        /// Registers the REST services into the DI container and builds a
        /// <see cref="RestRouteRegistry"/> singleton that the middleware
        /// consumes at request time.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="restServiceBuilder">A delegate that configures the REST services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRestServices(this IServiceCollection services,
                                                        Func<RestServiceBuilder, RestServiceBuilder> restServiceBuilder)
        {
            var builder = new RestServiceBuilder(services);
            builder = restServiceBuilder(builder);

            // Build the route table from the full service collection (after the
            // caller has registered all REST services) and register it as a
            // singleton so the middleware can resolve it from DI instead of
            // relying on a captured static reference.
            services.AddSingleton(sp => new RestRouteRegistry(services));

            return services;
        }

        /// <summary>
        /// Adds the <see cref="RestMiddleware"/> to the request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseRestMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RestMiddleware>();
        }
    }
}
