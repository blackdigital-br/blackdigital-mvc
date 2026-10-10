using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Rest
{
    public static class RestMiddlewareExtensions
    {
        /// <summary>
        /// Registers the REST services into the DI container and builds a
        /// <see cref="RestRouteRegistry"/> singleton that the middleware
        /// consumes at request time.
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="restServiceBuilder">Função para configurar os serviços REST</param>
        /// <returns>Coleção de serviços</returns>
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
        /// Adiciona o middleware REST ao pipeline de requisições
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseRestMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RestMiddleware>();
        }
    }
}
