using System.Reflection;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Helper methods for extracting route, HTTP method and authorization
    /// information from BlackDigital.Rest action attributes via reflection.
    /// </summary>
    public static class ActionAttributeHelper
    {

        /// <summary>
        /// Gets the route template declared on an action attribute by inspecting
        /// common property/field names (<c>Path</c>, <c>Route</c>, <c>Template</c>,
        /// <c>Value</c>).
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns>The route template, or an empty string when none is found.</returns>
        public static string GetRoute(object actionAttribute)
        {
            var type = actionAttribute.GetType();
            var names = new[] { "Path", "Route", "Template", "Value" };
            foreach (var name in names)
            {
                var prop = type.GetProperty(name);
                if (prop != null)
                {
                    var val = prop.GetValue(actionAttribute) as string;
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(actionAttribute) as string;
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
            }
            return "";
        }

        /// <summary>
        /// Gets the HTTP operation type declared on an action attribute by
        /// inspecting a <c>Method</c> or <c>RestMethod</c> property/field.
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns>The resolved HTTP method; defaults to <see cref="HttpMethod.Get"/>.</returns>
        public static HttpMethod GetOperationType(object actionAttribute)
        {
            var type = actionAttribute.GetType();
            var methodProp = type.GetProperty("Method") ?? type.GetProperty("RestMethod");
            object? methodVal = null;
            if (methodProp != null)
                methodVal = methodProp.GetValue(actionAttribute);
            else
            {
                var field = type.GetField("Method", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? type.GetField("RestMethod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    methodVal = field.GetValue(actionAttribute);
            }

            if (methodVal == null)
                return HttpMethod.Get;

            var name = methodVal.ToString()?.ToLowerInvariant() ?? "";
            return name switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                "patch" => HttpMethod.Patch,
                "head" => HttpMethod.Head,
                "options" => HttpMethod.Options,
                _ => HttpMethod.Get
            };
        }

        /// <summary>
        /// Determines whether an action attribute declares that authorization is
        /// required by inspecting a boolean <c>Authorize</c> property or field.
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns><c>true</c> when authorization is required; otherwise, <c>false</c>.</returns>
        public static bool GetAuthorize(object actionAttribute)
        {
            var type = actionAttribute.GetType();
            var prop = type.GetProperty("Authorize");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                var val = prop.GetValue(actionAttribute);
                return val is true;
            }
            var field = type.GetField("Authorize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                var val = field.GetValue(actionAttribute);
                return val is true;
            }
            return false;
        }

    }
}
