using System.Reflection;

namespace BlackDigital.AspNet.OpenApi
{
    public static class ActionAttributeHelper
    {

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
