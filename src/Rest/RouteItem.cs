using System.Reflection;

namespace BlackDigital.Mvc.Rest
{
    public struct RouteItem
    {
        public PathRouteItem[] Path { get; set; }
        public string HttpMethod { get; set; }
        public bool Authorize { get; set; }

        public Type DomainType { get; set; }
        public MethodInfo Method { get; set; }


        public readonly bool IsMatch(string method, string? route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            var routeSegments = route.Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            return IsMatch(method, routeSegments);
        }

        public readonly bool IsMatch(string method, IEnumerable<string> routeSegments)
        {
            if (routeSegments == null || !routeSegments.Any())
                return false;

            if (HttpMethod.ToLower() != method.ToLower())
                return false;

            if (routeSegments.Count() != Path.Length)
                return false;

            return Path.Select((p, i) => p.IsMatch(routeSegments.ElementAt(i))).All(b => b);
        }
    }
}
