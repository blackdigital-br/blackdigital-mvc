using System.Reflection;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;


namespace BlackDigital.Mvc.Rest
{
    internal class ActionServiceItem
    {
        internal ActionServiceItem(RestServiceItem service, MethodInfo method)
        {
            Service = service;
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ActionAttribute = method.GetCustomAttribute<ActionAttribute>() 
                                        ?? throw new NullReferenceException(nameof(ActionAttribute));

            Parameters = method.GetParameters().Select(p => new ParameterActionItem(p)).ToArray();
        }

        private readonly MethodInfo Method;
        private readonly RestServiceItem Service;
        private readonly ActionAttribute ActionAttribute;
        private readonly ParameterActionItem[] Parameters;

        internal bool IsMatch(HttpContext context, string scopedPath)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            return ValidateMethod(context)
                    && ValidateAuthorization(context)
                    && ValidateRoute(scopedPath);
        }

        private bool ValidateMethod(HttpContext context)
        {
            return context.Request.Method.ToUpper() == Enum.GetName(ActionAttribute.Method)?.ToUpper();
        }

        private bool ValidateAuthorization(HttpContext context)
        {
            if (ActionAttribute.Authorize)
                return context.User.Identity?.IsAuthenticated ?? false;

            return true;
        }

        private bool ValidateRoute(string scopedPath)
        {
            string[]? pathItens;

            if (string.IsNullOrWhiteSpace(scopedPath))
                pathItens = Array.Empty<string>();
            else
                pathItens = scopedPath.Split('/');

            var routeParameters = Parameters.Where(p => p.Route != null)
                                            .ToArray();

            var actionRoutes = ActionAttribute.Route?.Split('/') ?? Array.Empty<string>();

            if (actionRoutes.Length != pathItens.Length)
                return false;

            foreach (var actionRoute in actionRoutes)
            {
                var currentPath = pathItens[actionRoutes.ToList().IndexOf(actionRoute)];

                if (Regex.IsMatch(actionRoute, @"{(.+?)}"))
                {
                    var routeParameter = routeParameters.FirstOrDefault(p => p.Route?.Name == actionRoute);

                    if (routeParameter is null)
                        return false;
                }
                else
                {
                    if (actionRoute != currentPath)
                        return false;
                }
            }

            return true;
        }


        internal async Task ExecuteActionAsync(HttpContext context, 
                                               IServiceProvider serviceProvider,
                                               string scopedPath)
        {
            var serviceInstance = serviceProvider.GetService(Service.Type);

            if (serviceInstance is null)
                throw new NullReferenceException(nameof(serviceInstance));

            try
            {
                var resultMethod = Method.Invoke(serviceInstance, await GetParameters(context, serviceProvider, scopedPath));

                if (Method.ReturnType != null)
                {
                    if (resultMethod is Task)
                    {
                            await (Task)resultMethod;

                            if (Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                                resultMethod = resultMethod.GetType().GetProperty("Result")?.GetValue(resultMethod);
                            else
                                resultMethod = null;
                    }

                    CreateResponse(context, resultMethod);
                }
                else
                {
                    context.Response.StatusCode = 200;
                }
            }
            catch (BusinessException businessException)
            { 
                CreateBusinessExceptionResponse(context, businessException);
            }
        }

        private async Task<object?[]> GetParameters(HttpContext context, IServiceProvider serviceProvider, string scopedPath)
        {
            List<object> arguments = new();

            string[]? pathItens;

            if (string.IsNullOrWhiteSpace(scopedPath))
                pathItens = Array.Empty<string>();
            else
                pathItens = scopedPath.Split('/');

            int pathPosition = 0;

            foreach (var parameter in Parameters)
            {
                if (parameter.Route != null)
                {
                    if (Regex.IsMatch(parameter.Route?.Name ?? string.Empty, @"{(.+?)}"))
                    {
                        var value = Regex.Replace(parameter.Route?.Name, @"{(.+?)}",
                            match =>
                            {
                                return pathItens[pathPosition++];
                            });

                        arguments.Add(Convert.ChangeType(value, parameter.Type));
                    }
                }
                
                if (parameter.Header != null)
                {
                    if (context.Request.Headers.TryGetValue(parameter.Header?.Name ?? parameter.Name, 
                                                    out var value))
                    {
                        arguments.Add(Convert.ChangeType(value, parameter.Type));
                    }
                }

                if (parameter.Query != null)
                {
                    if (parameter.Query.Type == QueryParameterType.Parameter)
                    {
                        if (context.Request.Query.TryGetValue(parameter.Query?.Name ?? parameter.Name,
                                                    out StringValues values))
                        {
                            //TODO: refactor
                            arguments.Add(Convert.ChangeType(values.First(), parameter.Type));
                        }
                    }
                    else
                    {
                        arguments.Add(context.Request.QueryString.ToString().FromQueryString(parameter.Type));
                    }
                }

                if (parameter.Body != null)
                {
                    context.Request.EnableBuffering();
                    var rawRequestBody = await (new StreamReader(context.Request.Body)).ReadToEndAsync();
                    context.Request.Body.Position = 0;
                    arguments.Add(JsonCast.To(rawRequestBody, parameter.Type));
                }
            }

            return arguments.ToArray();
        }

        private static void CreateResponse(HttpContext context, object? returnValue)
        {
            if (returnValue != null)
            {
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(JsonCast.ToJson(returnValue));
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }

        private static void CreateBusinessExceptionResponse(HttpContext context, BusinessException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.WriteAsync(JsonCast.ToJson(exception.Message));
            context.Response.StatusCode = exception.Code;
        }
    }
}

