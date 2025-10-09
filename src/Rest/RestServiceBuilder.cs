using Microsoft.Extensions.DependencyInjection;


namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Builder para configurar servi�os REST
    /// </summary>
    public class RestServiceBuilder
    {
        private readonly IServiceCollection _services;

        public RestServiceBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Adiciona um servi�o REST com sua implementa��o
        /// </summary>
        /// <typeparam name="TService">Interface do servi�o</typeparam>
        /// <typeparam name="TImplementation">Implementa��o do servi�o</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddScoped<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um servi�o REST com lifetime singleton
        /// </summary>
        /// <typeparam name="TService">Interface do servi�o</typeparam>
        /// <typeparam name="TImplementation">Implementa��o do servi�o</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddSingletonService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddSingleton<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um servi�o REST com lifetime transient
        /// </summary>
        /// <typeparam name="TService">Interface do servi�o</typeparam>
        /// <typeparam name="TImplementation">Implementa��o do servi�o</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddTransientService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddTransient<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um servi�o REST usando uma factory
        /// </summary>
        /// <typeparam name="TService">Interface do servi�o</typeparam>
        /// <param name="factory">Factory para criar a inst�ncia</param>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddService<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            _services.AddScoped(factory);
            return this;
        }
    }
}
