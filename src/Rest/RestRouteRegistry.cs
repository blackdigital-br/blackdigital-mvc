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
    /// <remarks>
    /// To keep request-time matching fast, routes are pre-processed at build time:
    /// <list type="bullet">
    /// <item>Each route's URL pattern is split into segments once.</item>
    /// <item>Routes without template parameters are stored in a dictionary keyed
    /// by the full lowercased path, enabling O(1) lookups for the common case.</item>
    /// <item>Parameterized routes are grouped by segment count so matching only
    /// considers candidates with the same number of segments as the request.</item>
    /// </list>
    /// </remarks>
    public sealed class RestRouteRegistry
    {
        private readonly Dictionary<string, List<RestRouteEntry>> _routesByMethod;
        private readonly Dictionary<string, Dictionary<string, RestRouteEntry>> _staticRoutesByMethod;
        private readonly Dictionary<string, Dictionary<int, List<RestRouteEntry>>> _parameterizedRoutesByMethodAndSegmentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestRouteRegistry"/> class
        /// by scanning the provided service descriptors for interfaces decorated
        /// with <c>ServiceAttribute</c>.
        /// </summary>
        /// <param name="services">The service descriptors to scan for REST services.</param>
        public RestRouteRegistry(IEnumerable<ServiceDescriptor> services)
        {
            _routesByMethod = BuildRoutes(services, out _staticRoutesByMethod, out _parameterizedRoutesByMethodAndSegmentCount);
        }

        /// <summary>
        /// Returns the route entries registered for the given HTTP method
        /// (e.g. "GET", "POST"), or an empty enumeration when none exist.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to look up (case-insensitive).</param>
        /// <returns>A read-only list of route entries for the method, or an empty list when none are registered.</returns>
        public IReadOnlyList<RestRouteEntry> GetRoutesForMethod(string httpMethod)
            => _routesByMethod.TryGetValue(httpMethod, out var routes) ? routes : Array.Empty<RestRouteEntry>();

        /// <summary>
        /// Returns the static (non-parameterized) routes for the given HTTP method
        /// keyed by their lowercased full path. Used for O(1) lookups of routes
        /// without template parameters.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to look up (case-insensitive).</param>
        /// <returns>A read-only dictionary mapping lowercased paths to route entries, or an empty dictionary when none exist.</returns>
        public IReadOnlyDictionary<string, RestRouteEntry> GetStaticRoutes(string httpMethod)
            => _staticRoutesByMethod.TryGetValue(httpMethod, out var routes) ? routes : EmptyStaticRoutes;

        /// <summary>
        /// Returns the parameterized routes for the given HTTP method grouped by
        /// the number of URL segments. Only routes with the same segment count as
        /// the request path need to be considered during matching.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to look up (case-insensitive).</param>
        /// <returns>A read-only dictionary mapping segment counts to route entry lists, or an empty dictionary when none exist.</returns>
        public IReadOnlyDictionary<int, IReadOnlyList<RestRouteEntry>> GetParameterizedRoutes(string httpMethod)
        {
            if (!_parameterizedRoutesByMethodAndSegmentCount.TryGetValue(httpMethod, out var bySegment))
                return EmptyParameterizedRoutes;

            var result = new Dictionary<int, IReadOnlyList<RestRouteEntry>>();
            foreach (var kvp in bySegment)
                result[kvp.Key] = kvp.Value;
            return result;
        }

        private static readonly Dictionary<string, RestRouteEntry> EmptyStaticRoutes = new();
        private static readonly Dictionary<int, IReadOnlyList<RestRouteEntry>> EmptyParameterizedRoutes = new();

        private static Dictionary<string, List<RestRouteEntry>> BuildRoutes(
            IEnumerable<ServiceDescriptor> services,
            out Dictionary<string, Dictionary<string, RestRouteEntry>> staticRoutesByMethod,
            out Dictionary<string, Dictionary<int, List<RestRouteEntry>>> parameterizedRoutesByMethodAndSegmentCount)
        {
            var routesByMethod = new Dictionary<string, List<RestRouteEntry>>(StringComparer.OrdinalIgnoreCase);
            staticRoutesByMethod = new Dictionary<string, Dictionary<string, RestRouteEntry>>(StringComparer.OrdinalIgnoreCase);
            parameterizedRoutesByMethodAndSegmentCount = new Dictionary<string, Dictionary<int, List<RestRouteEntry>>>(StringComparer.OrdinalIgnoreCase);

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

                    // Bucket the route into either the static-lookup dictionary
                    // or the parameterized group-by-segment-count structure.
                    if (entry.HasParameters)
                    {
                        if (!parameterizedRoutesByMethodAndSegmentCount.TryGetValue(httpMethod, out var bySegment))
                        {
                            bySegment = new Dictionary<int, List<RestRouteEntry>>();
                            parameterizedRoutesByMethodAndSegmentCount[httpMethod] = bySegment;
                        }

                        if (!bySegment.TryGetValue(entry.Segments.Length, out var paramList))
                        {
                            paramList = new List<RestRouteEntry>();
                            bySegment[entry.Segments.Length] = paramList;
                        }

                        paramList.Add(entry);
                    }
                    else
                    {
                        if (!staticRoutesByMethod.TryGetValue(httpMethod, out var staticDict))
                        {
                            staticDict = new Dictionary<string, RestRouteEntry>(StringComparer.OrdinalIgnoreCase);
                            staticRoutesByMethod[httpMethod] = staticDict;
                        }

                        staticDict[route] = entry;
                    }
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
    /// <remarks>
    /// The URL pattern is pre-split into <see cref="Segments"/> at registry build
    /// time so request-time matching does not allocate arrays by splitting the
    /// pattern string on every request.
    /// </remarks>
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

            // Pre-split the pattern into segments and detect template parameters
            // once at build time, avoiding per-request string splitting.
            Segments = routePattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
            HasParameters = Segments.Any(s => s.StartsWith("{") && s.EndsWith("}"));
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

        /// <summary>
        /// Gets the URL pattern pre-split into segments (without empty entries).
        /// Computed once at build time to avoid per-request allocations.
        /// </summary>
        public string[] Segments { get; }

        /// <summary>
        /// Gets a value indicating whether the route pattern contains template
        /// parameters (e.g. <c>{id}</c>). Static routes can be looked up in O(1)
        /// from a dictionary; parameterized routes require segment-by-segment
        /// matching.
        /// </summary>
        public bool HasParameters { get; }
    }
}
