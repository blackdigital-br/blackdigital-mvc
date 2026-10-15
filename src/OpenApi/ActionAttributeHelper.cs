using System.Reflection;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Helper methods for extracting route, HTTP method, authorization,
    /// policy, name and description information from BlackDigital.Rest action
    /// attributes via reflection.
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

        /// <summary>
        /// Gets the authorization policy name declared on an action attribute by
        /// inspecting a <c>Policy</c> property or field.
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns>The policy name, or <c>null</c> when none is declared.</returns>
        public static string? GetPolicy(object actionAttribute)
        {
            var type = actionAttribute.GetType();

            var prop = type.GetProperty("Policy");
            if (prop != null && prop.PropertyType == typeof(string))
            {
                var val = prop.GetValue(actionAttribute) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            var field = type.GetField("Policy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string))
            {
                var val = field.GetValue(actionAttribute) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return null;
        }

        /// <summary>
        /// Gets the display name of an action by invoking its <c>GetName</c>
        /// method (which resolves the localized name using the configured
        /// <c>ResourceType</c>).
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns>The resolved action name, or <c>null</c> when none is declared.</returns>
        public static string? GetName(object actionAttribute)
        {
            var type = actionAttribute.GetType();

            var method = type.GetMethod("GetName", Type.EmptyTypes);
            if (method != null && method.ReturnType == typeof(string))
            {
                var val = method.Invoke(actionAttribute, null) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return null;
        }

        /// <summary>
        /// Gets the description of an action by invoking its <c>GetDescription</c>
        /// method (which resolves the localized description using the configured
        /// <c>ResourceType</c>).
        /// </summary>
        /// <param name="actionAttribute">The action attribute instance to inspect.</param>
        /// <returns>The resolved action description, or <c>null</c> when none is declared.</returns>
        public static string? GetDescription(object actionAttribute)
        {
            var type = actionAttribute.GetType();

            var method = type.GetMethod("GetDescription", Type.EmptyTypes);
            if (method != null && method.ReturnType == typeof(string))
            {
                var val = method.Invoke(actionAttribute, null) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return null;
        }

    }
}
