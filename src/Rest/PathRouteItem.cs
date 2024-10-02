
using Microsoft.AspNetCore.Routing;

namespace BlackDigital.Mvc.Rest
{
    public struct PathRouteItem
    {
        public string Name { get; set; }

        public Type? TypePath { get; set; }

        public readonly bool IsMatch(string route)
        {
            if (TypePath == null && route.ToLower() == Name)
                return true;

            if (TypePath == typeof(int))
                return int.TryParse(route, out _);

            return false;
        }
    }
}
