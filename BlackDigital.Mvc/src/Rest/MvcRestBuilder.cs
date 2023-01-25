using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public class MvcRestBuilder
    {
        public MvcRestBuilder(IServiceCollection serviceCollection)
        {
            Services = new();
            ServiceCollection = serviceCollection;
        }

        protected internal List<Type> Services { get; private set; }
        protected IServiceCollection ServiceCollection { get; private set; }

        public MvcRestBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            ServiceCollection.AddScoped<TService, TImplementation>();
            Services.Add(typeof(TService));
            return this;
        }
    }
}
