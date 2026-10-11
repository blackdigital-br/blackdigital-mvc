using BlackDigital.Rest;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Holds the pre-built route table for BlackDigital.Rest services. Built once
    /// during <c>AddRestServices</c> from the registered <see cref="IServiceCollection"/>
    /// and resolved by the <see cref="RestMiddleware"/> at request time, replacing
    /// the previous static-field capture that broke multi-host and parallel-test
    /// scenarios.
    /// </summary>
    public sealed class RestRouteRegistry
    {
        private readonly Dictionary<string, List<RestRouteEntry>> _routesByMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestRouteRegistry"/> class
        /// by scanning the provided service descriptors for interfaces decorated
        /// with <c>ServiceAttribute</c>.
        /// </summary>
        /// <param name="services">The service descriptors to scan for REST services.</param>
        public RestRouteRegistry(IEnumerable<ServiceDescriptor> services)
        {
            _routesByMethod = BuildRoutes(services);
        }

        /// <summary>
        /// Returns the route entries registered for the given HTTP method
        /// (e.g. "GET", "POST"), or an empty enumeration when none exist.
        /// </summary>
        public IReadOnlyList<RestRouteEntry> GetRoutesForMethod(string httpMethod)
            => _routesByMethod.TryGetValue(httpMethod, out var routes) ? routes : Array.Empty<RestRouteEntry>();

        private static Dictionary<string, List<RestRouteEntry>> BuildRoutes(IEnumerable<ServiceDescriptor> services)
        {
            var routesByMethod = new Dictionary<string, List<RestRouteEntry>>(StringComparer.OrdinalIgnoreCase);

            foreach (var service in services)
            {
                if (!service.ServiceType.IsInterface)
                    continue;

                var serviceAttribute = service.ServiceType.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttribute == null)
                    continue;

                var baseRoute = serviceAttribute.BaseRoute.ToLowerInvariant();
                if (!baseRoute.StartsWith("/"))
                    baseRoute = "/" + baseRoute;

                foreach (var method in service.ServiceType.GetMethods())
                {
                    var actionAttribute = method.GetCustomAttribute<ActionAttribute>();
                    if (actionAttribute == null)
                        continue;

                    var httpMethod = actionAttribute.Method.ToString().ToUpperInvariant();
                    var route = BuildFullRoute(baseRoute, actionAttribute.Route ?? string.Empty);

                    var entry = new RestRouteEntry(route, service, method);

                    if (!routesByMethod.TryGetValue(httpMethod, out var list))
                    {
                        list = new List<RestRouteEntry>();
                        routesByMethod[httpMethod] = list;
                    }

                    list.Add(entry);
                }
            }

            return routesByMethod;
        }

        private static string BuildFullRoute(string baseRoute, string actionRoute)
        {
            if (string.IsNullOrEmpty(actionRoute))
                return baseRoute;

            var route = actionRoute;
            if (!route.StartsWith("/"))
                route = "/" + route;

            return baseRoute + route;
        }
    }

    /// <summary>
    /// A single registered REST route: its URL pattern, the service descriptor
    /// that owns it, and the method to invoke.
    /// </summary>
    public sealed class RestRouteEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestRouteEntry"/> class.
        /// </summary>
        /// <param name="routePattern">The URL pattern for the route.</param>
        /// <param name="service">The service descriptor that owns the route.</param>
        /// <param name="method">The method to invoke when the route matches.</param>
        public RestRouteEntry(string routePattern, ServiceDescriptor service, MethodInfo method)
        {
            RoutePattern = routePattern;
            Service = service;
            Method = method;
        }

        /// <summary>
        /// Gets the URL pattern for the route.
        /// </summary>
        public string RoutePattern { get; }

        /// <summary>
        /// Gets the service descriptor that owns the route.
        /// </summary>
        public ServiceDescriptor Service { get; }

        /// <summary>
        /// Gets the method to invoke when the route matches.
        /// </summary>
        public MethodInfo Method { get; }
    }
}
