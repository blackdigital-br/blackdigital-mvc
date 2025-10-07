using Microsoft.Extensions.DependencyInjection;


namespace BlackDigital.Mvc.Rest
{
    /// <summary>
    /// Builder para configurar serviços REST
    /// </summary>
    public class RestServiceBuilder
    {
        private readonly IServiceCollection _services;

        public RestServiceBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Adiciona um serviço REST com sua implementação
        /// </summary>
        /// <typeparam name="TService">Interface do serviço</typeparam>
        /// <typeparam name="TImplementation">Implementação do serviço</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddScoped<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um serviço REST com lifetime singleton
        /// </summary>
        /// <typeparam name="TService">Interface do serviço</typeparam>
        /// <typeparam name="TImplementation">Implementação do serviço</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddSingletonService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddSingleton<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um serviço REST com lifetime transient
        /// </summary>
        /// <typeparam name="TService">Interface do serviço</typeparam>
        /// <typeparam name="TImplementation">Implementação do serviço</typeparam>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddTransientService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddTransient<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adiciona um serviço REST usando uma factory
        /// </summary>
        /// <typeparam name="TService">Interface do serviço</typeparam>
        /// <param name="factory">Factory para criar a instância</param>
        /// <returns>Builder para encadeamento</returns>
        public RestServiceBuilder AddService<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            _services.AddScoped(factory);
            return this;
        }
    }
}
