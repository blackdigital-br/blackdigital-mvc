using BlackDigital.Rest;
using System.Reflection;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Helper methods for extracting route, name and description information
    /// from BlackDigital.Rest service attributes via reflection.
    /// </summary>
    public static class ServiceAttributeHelper
    {

        /// <summary>
        /// Gets the base route declared on a service attribute by inspecting
        /// common property/field names (<c>BaseRoute</c>, <c>Route</c>,
        /// <c>Path</c>, <c>Value</c>) and falling back to the attribute's
        /// <c>ToString</c> representation.
        /// </summary>
        /// <param name="serviceAttribute">The service attribute instance to inspect.</param>
        /// <returns>The resolved base route, or <c>"api/unknown"</c> when none is found.</returns>
        public static string GetRoute(ServiceAttribute serviceAttribute)
        {
            var type = serviceAttribute.GetType();

            var baseRouteProperty = type.GetProperty("BaseRoute");
            if (baseRouteProperty != null)
            {
                var baseRoute = baseRouteProperty.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(baseRoute))
                    return baseRoute;
            }

            var routeProperty = type.GetProperty("Route");
            if (routeProperty != null)
            {
                var route = routeProperty.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(route))
                    return route;
            }

            var pathProperty = type.GetProperty("Path");
            if (pathProperty != null)
            {
                var path = pathProperty.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            var valueProperty = type.GetProperty("Value");
            if (valueProperty != null)
            {
                var value = valueProperty.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            var routeField = type.GetField("Route", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (routeField != null)
            {
                var route = routeField.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(route))
                    return route;
            }

            var pathField = type.GetField("Path", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pathField != null)
            {
                var path = pathField.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            var valueField = type.GetField("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (valueField != null)
            {
                var value = valueField.GetValue(serviceAttribute) as string;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            var toString = serviceAttribute.ToString();
            if (!string.IsNullOrEmpty(toString) && toString != serviceAttribute.GetType().FullName)
                return toString;

            return "api/unknown";
        }

        /// <summary>
        /// Gets the display name of a service by invoking its <c>GetServiceName</c>
        /// method (which resolves the localized name using the configured
        /// <c>ResourceType</c>).
        /// </summary>
        /// <param name="serviceAttribute">The service attribute instance to inspect.</param>
        /// <returns>The resolved service name, or <c>null</c> when none is declared.</returns>
        public static string? GetName(ServiceAttribute serviceAttribute)
        {
            var type = serviceAttribute.GetType();

            var method = type.GetMethod("GetServiceName", Type.EmptyTypes);
            if (method != null && method.ReturnType == typeof(string))
            {
                var val = method.Invoke(serviceAttribute, null) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return null;
        }

        /// <summary>
        /// Gets the description of a service by invoking its
        /// <c>GetServiceDescription</c> method (which resolves the localized
        /// description using the configured <c>ResourceType</c>).
        /// </summary>
        /// <param name="serviceAttribute">The service attribute instance to inspect.</param>
        /// <returns>The resolved service description, or <c>null</c> when none is declared.</returns>
        public static string? GetDescription(ServiceAttribute serviceAttribute)
        {
            var type = serviceAttribute.GetType();

            var method = type.GetMethod("GetServiceDescription", Type.EmptyTypes);
            if (method != null && method.ReturnType == typeof(string))
            {
                var val = method.Invoke(serviceAttribute, null) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return null;
        }

    }
}
