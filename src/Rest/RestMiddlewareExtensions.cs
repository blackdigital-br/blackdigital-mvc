using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public static class RestMiddlewareExtensions
    {
        private static IServiceCollection? _capturedServices;

        /// <summary>
        /// Adiciona os serviços REST ao container de DI e captura a coleção para uso no middleware
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="restServiceBuilder">Função para configurar os serviços REST</param>
        /// <returns>Coleção de serviços</returns>
        public static IServiceCollection AddRestServices(this IServiceCollection services,
                                                        Func<RestServiceBuilder, RestServiceBuilder> restServiceBuilder)
        {
            var builder = new RestServiceBuilder(services);
            builder = restServiceBuilder(builder);
            
            // Capturar a coleção de serviços para uso no middleware
            _capturedServices = services;
            
            return services;
        }

        /// <summary>
        /// Adiciona o middleware REST ao pipeline de requisições
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseRestMiddleware(this IApplicationBuilder app)
        {
            if (_capturedServices == null)
                throw new InvalidOperationException("You must call AddRestServices before UseRestMiddleware");

            return app.UseMiddleware<RestMiddleware>(_capturedServices);
        }
    }
}