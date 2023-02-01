using BlackDigital.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

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

            if (routeParameters.Length != pathItens.Length)
                return false;

            for (int i = 0; i < pathItens.Length; i++)
            {
                var value = pathItens[i];
                var routeParameter = routeParameters[i];

                if (!Regex.IsMatch(routeParameter?.Route?.Name ?? string.Empty, @"{(.+?)}")
                    && value != routeParameter?.Route?.Name)
                {
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

            var resultMethod = Method.Invoke(serviceInstance, GetParameters(context, serviceProvider, scopedPath));

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

        private object?[] GetParameters(HttpContext context, IServiceProvider serviceProvider, string scopedPath)
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
                    context.Request.Body.Position = 0;
                    var rawRequestBody = new StreamReader(context.Request.Body).ReadToEnd();
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
    }
}

