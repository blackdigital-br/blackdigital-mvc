using Microsoft.Extensions.DependencyInjection;


namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Builder used to configure REST services and their lifetimes within the
    /// dependency injection container.
    /// </summary>
    public class RestServiceBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestServiceBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        public RestServiceBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Registers a REST service with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <typeparam name="TImplementation">The service implementation.</typeparam>
        /// <returns>The current builder for chaining.</returns>
        public RestServiceBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddScoped<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a REST service with a singleton lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <typeparam name="TImplementation">The service implementation.</typeparam>
        /// <returns>The current builder for chaining.</returns>
        public RestServiceBuilder AddSingletonService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddSingleton<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a REST service with a transient lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <typeparam name="TImplementation">The service implementation.</typeparam>
        /// <returns>The current builder for chaining.</returns>
        public RestServiceBuilder AddTransientService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddTransient<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a REST service using a factory delegate with a scoped lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <param name="factory">The factory used to create the service instance.</param>
        /// <returns>The current builder for chaining.</returns>
        public RestServiceBuilder AddService<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            _services.AddScoped(factory);
            return this;
        }
    }
}
