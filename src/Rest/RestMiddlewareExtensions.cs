using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

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
            return AddRestServices(services, restServiceBuilder, _ => { });
        }

        /// <summary>
        /// Registers the REST services into the DI container, configures the
        /// <see cref="RestOptions"/> that control optional middleware behavior
        /// (such as request logging), and builds a
        /// <see cref="RestRouteRegistry"/> singleton that the middleware
        /// consumes at request time.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="restServiceBuilder">A delegate that configures the REST services.</param>
        /// <param name="configureOptions">A delegate that configures the REST middleware options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRestServices(this IServiceCollection services,
                                                        Func<RestServiceBuilder, RestServiceBuilder> restServiceBuilder,
                                                        Action<RestOptions> configureOptions)
        {
            var builder = new RestServiceBuilder(services);
            builder = restServiceBuilder(builder);

            // Register RestOptions so the middleware can resolve IOptions<RestOptions>.
            services.Configure(configureOptions);

            // Build the route table from the full service collection (after the
            // caller has registered all REST services) and register it as a
            // singleton so the middleware can resolve it from DI instead of
            // relying on a captured static reference.
            services.AddSingleton(sp => new RestRouteRegistry(services));

            // Register any [Transform] attributes declared on REST service
            // methods into a shared TransformBuilder so they are picked up by
            // the TransformManager at request time.
            RegisterTransformAttributes(services);

            return services;
        }

        /// <summary>
        /// Scans the registered REST service interfaces for methods decorated
        /// with <see cref="TransformAttribute"/> and registers the declared
        /// rules into a shared <see cref="TransformBuilder"/> singleton.
        /// </summary>
        /// <param name="services">The service collection to scan and register into.</param>
        private static void RegisterTransformAttributes(IServiceCollection services)
        {
            // Reuse an existing TransformBuilder (e.g. registered by AddTransform)
            // or create a new one so that programmatic and attribute-based rules
            // share the same registry.
            var transformBuilderDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TransformBuilder));
            TransformBuilder transformBuilder;
            if (transformBuilderDescriptor?.ImplementationInstance is TransformBuilder existing)
            {
                transformBuilder = existing;
            }
            else
            {
                transformBuilder = new TransformBuilder();
                services.AddSingleton(transformBuilder);
                services.TryAddScoped<TransformManager>();
            }

            foreach (var service in services)
            {
                if (!service.ServiceType.IsInterface)
                    continue;

                var serviceAttribute = service.ServiceType.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttribute == null)
                    continue;

                var baseRoute = serviceAttribute.BaseRoute ?? string.Empty;

                foreach (var method in service.ServiceType.GetMethods())
                {
                    var actionAttribute = method.GetCustomAttribute<ActionAttribute>();
                    if (actionAttribute == null)
                        continue;

                    var transforms = method.GetCustomAttributes<TransformAttribute>();
                    if (!transforms.Any())
                        continue;

                    // The action key must match the key computed at runtime by
                    // RestMiddleware (KeyIdentifier or METHOD:baseRoute/route).
                    var actionKey = actionAttribute.KeyIdentifier
                        ?? $"{actionAttribute.Method.ToString().ToUpperInvariant()}:{baseRoute}/{actionAttribute.Route}";

                    foreach (var transform in transforms)
                    {
                        var key = new TransformKey(actionKey, transform.Version, transform.Direction);
                        transformBuilder.AddRule(key, transform.RuleType);
                    }
                }
            }
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
