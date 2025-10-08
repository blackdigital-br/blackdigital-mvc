using BlackDigital.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace BlackDigital.Mvc.Rest
{
    /// <summary>
    /// Middleware personalizado que intercepta requisições HTTP e as roteia para serviços baseados em atributos de rota e ação,
    /// fornecendo funcionalidades REST automáticas. Este middleware analisa os serviços registrados que possuem ServiceAttribute
    /// e ActionAttribute, criando rotas dinâmicas e executando os métodos correspondentes com base na URL e método HTTP da requisição.
    /// </summary>
    public class RestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Dictionary<string, ServiceDescriptor> _services;
        private readonly Dictionary<string, MethodInfo> _methodCache;

        public RestMiddleware(RequestDelegate next, IServiceCollection services)
        {
            _next = next;
            _services = new Dictionary<string, ServiceDescriptor>();
            _methodCache = new Dictionary<string, MethodInfo>();
            
            BuildServiceRoutes(services);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            var method = context.Request.Method.ToUpper();

            if (TryMatchRoute(path, method, out var serviceDescriptor, out var methodInfo, out var routeValues))
            {
                await HandleRestRequest(context, serviceDescriptor, methodInfo, routeValues);
                return;
            }

            await _next(context);
        }

        private void BuildServiceRoutes(IServiceCollection services)
        {
            foreach (var service in services)
            {
                if (service.ServiceType.IsInterface)
                {
                    var serviceAttribute = service.ServiceType.GetCustomAttribute<ServiceAttribute>();
                    if (serviceAttribute != null)
                    {
                        var baseRoute = serviceAttribute.BaseRoute.ToLower();
                        if (!baseRoute.StartsWith("/"))
                            baseRoute = "/" + baseRoute;

                        foreach (var method in service.ServiceType.GetMethods())
                        {
                            var actionAttribute = method.GetCustomAttribute<ActionAttribute>();
                            if (actionAttribute != null)
                            {
                                var httpMethod = actionAttribute.Method.ToString().ToUpper();
                                var route = BuildFullRoute(baseRoute, actionAttribute.Route);
                                var key = $"{httpMethod}:{route}";

                                _services[key] = service;
                                _methodCache[key] = method;
                            }
                        }
                    }
                }
            }
        }

        private string BuildFullRoute(string baseRoute, string actionRoute)
        {
            if (string.IsNullOrEmpty(actionRoute))
                return baseRoute;

            var route = actionRoute;
            if (!route.StartsWith("/"))
                route = "/" + route;

            return baseRoute + route;
        }

        private bool TryMatchRoute(string path, string httpMethod, out ServiceDescriptor serviceDescriptor, 
                                 out MethodInfo methodInfo, out Dictionary<string, object> routeValues)
        {
            serviceDescriptor = null;
            methodInfo = null;
            routeValues = new Dictionary<string, object>();

            foreach (var kvp in _services)
            {
                var routeKey = kvp.Key;
                var parts = routeKey.Split(':');
                var routeMethod = parts[0];
                var routePattern = parts[1];

                if (routeMethod == httpMethod && MatchesPattern(path, routePattern, routeValues))
                {
                    serviceDescriptor = kvp.Value;
                    methodInfo = _methodCache[routeKey];
                    return true;
                }
            }

            return false;
        }

        private bool MatchesPattern(string path, string pattern, Dictionary<string, object> routeValues)
        {
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Length != patternSegments.Length)
                return false;

            for (int i = 0; i < patternSegments.Length; i++)
            {
                var patternSegment = patternSegments[i];
                var pathSegment = pathSegments[i];

                if (patternSegment.StartsWith("{") && patternSegment.EndsWith("}"))
                {
                    var paramName = patternSegment.Trim('{', '}');
                    routeValues[paramName] = pathSegment;
                }
                else if (!string.Equals(patternSegment, pathSegment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task HandleRestRequest(HttpContext context, ServiceDescriptor serviceDescriptor, 
                                           MethodInfo methodInfo, Dictionary<string, object> routeValues)
        {
            try
            {
                // Verificar autorização
                var actionAttribute = methodInfo.GetCustomAttribute<ActionAttribute>();
                var serviceAttribute = serviceDescriptor.ServiceType.GetCustomAttribute<ServiceAttribute>();
                
                if ((actionAttribute?.Authorize == true) || (serviceAttribute?.Authorize == true))
                {
                    if (!context.User.Identity.IsAuthenticated)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized");
                        return;
                    }
                }

                // Obter instância do serviço
                var service = context.RequestServices.GetService(serviceDescriptor.ServiceType);
                if (service == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Service not found");
                    return;
                }

                // Preparar parâmetros
                var parameters = await PrepareMethodParameters(context, methodInfo, routeValues);

                // Executar método
                var result = methodInfo.Invoke(service, parameters);

                // Processar resultado
                await ProcessResult(context, result, methodInfo.ReturnType);
            }
            catch (BusinessException businessException)
            {
                context.Response.StatusCode = businessException.Code;
                await context.Response.WriteAsync(businessException.Message);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is BusinessException innerBusinessException)
                {
                    context.Response.StatusCode = innerBusinessException.Code;
                    await context.Response.WriteAsync(innerBusinessException.Message);
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Internal server error");
                }
            }
        }

        
        private JsonSerializerOptions getJsonOptions(HttpContext context)
        {
            var jsonOptions = context.RequestServices
                                .GetService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                                ?.Value?.JsonSerializerOptions
                                ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

            return jsonOptions;
        }

        private async Task<object[]> PrepareMethodParameters(HttpContext context, MethodInfo methodInfo, 
                                                           Dictionary<string, object> routeValues)
        {
            var jsonOptions = getJsonOptions(context);

            var parameters = methodInfo.GetParameters();
            var values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterName = parameter.Name;

                // Verificar atributos de binding
                var routeAttribute = parameter.GetCustomAttribute(typeof(PathAttribute)) as PathAttribute;
                var bodyAttribute = parameter.GetCustomAttribute(typeof(BodyAttribute)) as BodyAttribute;
                var headerAttribute = parameter.GetCustomAttribute(typeof(HeaderAttribute)) as HeaderAttribute;
                var queryAttribute = parameter.GetCustomAttribute(typeof(QueryAttribute)) as QueryAttribute;

                if (routeAttribute != null)
                {
                    var routeKey = routeAttribute.Name ?? parameterName;
                    if (routeValues.ContainsKey(routeKey))
                    {
                        values[i] = ConvertValue(routeValues[routeKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (bodyAttribute != null)
                {
                    var body = await ReadRequestBody(context);
                    values[i] = System.Text.Json.JsonSerializer.Deserialize(body, parameter.ParameterType, jsonOptions);
                }
                else if (headerAttribute != null)
                {
                    var headerKey = headerAttribute.Name ?? parameterName;
                    if (context.Request.Headers.ContainsKey(headerKey))
                    {
                        values[i] = ConvertValue(context.Request.Headers[headerKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (queryAttribute != null)
                {
                    var queryKey = queryAttribute.Name ?? parameterName;
                    if (context.Request.Query.ContainsKey(queryKey))
                    {
                        values[i] = ConvertValue(context.Request.Query[queryKey].ToString(), parameter.ParameterType);
                    }
                }
                else
                {
                    // Padrão: query parameter
                    if (context.Request.Query.ContainsKey(parameterName))
                    {
                        values[i] = ConvertValue(context.Request.Query[parameterName].ToString(), parameter.ParameterType);
                    }
                }
            }

            return values;
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.Parse(value);

            if (targetType == typeof(long) || targetType == typeof(long?))
                return long.Parse(value);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.Parse(value);

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return DateTime.Parse(value);

            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
                return Guid.Parse(value);

            // Para tipos customizados como Id da BlackDigital
            if (targetType.Name == "Id")
            {
                return Activator.CreateInstance(targetType, value);
            }

            return Convert.ChangeType(value, targetType);
        }

        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            
            context.Request.Body.Position = 0;
            return body;
        }

        private async Task ProcessResult(HttpContext context, object result, Type returnType)
        {
            var jsonOptions = getJsonOptions(context);
            context.Response.ContentType = "application/json";

            if (returnType == typeof(void))
            {
                context.Response.StatusCode = 200;
                return;
            }

            if (result is Task task)
            {
                await task;

                if (returnType.IsGenericType)
                {
                    var taskResult = returnType.GetProperty("Result")?.GetValue(task);
                    if (taskResult != null)
                    {
                        var json = JsonSerializer.Serialize(taskResult, jsonOptions);
                        await context.Response.WriteAsync(json);
                    }
                }
            }
            else
            {
                var json = JsonSerializer.Serialize(result, jsonOptions);
                await context.Response.WriteAsync(json);
            }
        }
    }
}