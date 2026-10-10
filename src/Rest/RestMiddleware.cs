using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.Model;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Middleware personalizado que intercepta requisições HTTP e as roteia para serviços baseados em atributos de rota e ação,
    /// fornecendo funcionalidades REST automáticas. Este middleware analisa os serviços registrados que possuem ServiceAttribute
    /// e ActionAttribute, criando rotas dinâmicas e executando os métodos correspondentes com base na URL e método HTTP da requisição.
    /// </summary>
    public class RestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RestRouteRegistry _routeRegistry;
        private readonly ILogger<RestMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public RestMiddleware(RequestDelegate next,
                              RestRouteRegistry routeRegistry,
                              ILogger<RestMiddleware> logger,
                              IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _routeRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path))
            {
                await _next(context);
                return;
            }

            path = path.ToLowerInvariant();
            var method = context.Request.Method.ToUpperInvariant();

            if (TryMatchRoute(path, method, out var routeEntry, out var routeValues))
            {
                await HandleRestRequest(context, routeEntry!, routeValues);
                return;
            }

            await _next(context);
        }

        private bool TryMatchRoute(string path, string httpMethod, out RestRouteEntry? routeEntry, out Dictionary<string, object> routeValues)
        {
            routeEntry = null;
            routeValues = new Dictionary<string, object>();

            foreach (var entry in _routeRegistry.GetRoutesForMethod(httpMethod))
            {
                if (MatchesPattern(path, entry.RoutePattern, routeValues))
                {
                    routeEntry = entry;
                    return true;
                }

                routeValues.Clear();
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

        private async Task HandleRestRequest(HttpContext context, RestRouteEntry routeEntry, Dictionary<string, object> routeValues)
        {
            var methodInfo = routeEntry.Method;
            var serviceDescriptor = routeEntry.Service;

            try
            {
                // Verificar autorização
                var actionAttribute = methodInfo.GetCustomAttribute<ActionAttribute>();
                var serviceAttribute = serviceDescriptor.ServiceType.GetCustomAttribute<ServiceAttribute>();

                var actionKey = actionAttribute?.KeyIdentifier ?? $"{actionAttribute?.Method.ToString().ToUpperInvariant()}:{serviceAttribute?.BaseRoute}/{actionAttribute?.Route}";

                if ((actionAttribute?.Authorize == true) || (serviceAttribute?.Authorize == true))
                {
                    if (!context.User.Identity?.IsAuthenticated ?? true)
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
                    throw new InvalidOperationException($"Service not found to: {serviceDescriptor.ServiceType}");
                }

                // Preparar parâmetros
                var parameters = await PrepareMethodParameters(actionKey, context, methodInfo, routeValues);

                // Executar método
                var result = methodInfo.Invoke(service, parameters);

                // Processar resultado
                await ProcessResult(actionKey, context, result, methodInfo.ReturnType);
            }
            catch (BusinessException businessException)
            {
                context.Response.StatusCode = businessException.Code;
                await context.Response.WriteAsync(businessException.Message);
            }
            catch (BadHttpRequestException badRequest)
            {
                context.Response.StatusCode = badRequest.StatusCode;
                await context.Response.WriteAsync(badRequest.Message);
            }
            catch (Exception ex)
            {
                // methodInfo.Invoke wraps the thrown exception in TargetInvocationException;
                // unwrap to the root business exception when present.
                var rootException = ex.GetBaseException();
                if (rootException is BusinessException innerBusinessException)
                {
                    context.Response.StatusCode = innerBusinessException.Code;
                    await context.Response.WriteAsync(innerBusinessException.Message);
                }
                else
                {
                    _logger.LogError(ex, "Error on process Rest Service");
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    // Never leak stack traces outside the development environment.
                    var includeStackTrace = _environment.IsDevelopment();
                    var error = new RestError()
                    {
                        Message = rootException.Message,
                        Stack = includeStackTrace ? rootException.StackTrace : null
                    };

                    var json = JsonCast.ToJson(error);

                    await context.Response.WriteAsync(json);
                }
            }
        }


        private JsonSerializerOptions GetJsonOptions(HttpContext context)
        {
            var jsonOptions = context.RequestServices
                                .GetService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                                ?.Value?.JsonSerializerOptions
                                ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

            return jsonOptions;
        }

        private async Task<object?[]> PrepareMethodParameters(string actionKey, HttpContext context, MethodInfo methodInfo,
                                                           Dictionary<string, object> routeValues)
        {
            var jsonOptions = GetJsonOptions(context);

            var parameters = methodInfo.GetParameters();
            var values = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterName = parameter.Name;

                // CancellationToken is bound to the request's aborted token so
                // long-running service methods respect client disconnects.
                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    values[i] = context.RequestAborted;
                    continue;
                }

                // Verificar atributos de binding
                var routeAttribute = parameter.GetCustomAttribute<PathAttribute>();
                var bodyAttribute = parameter.GetCustomAttribute<BodyAttribute>();
                var headerAttribute = parameter.GetCustomAttribute<HeaderAttribute>();
                var queryAttribute = parameter.GetCustomAttribute<QueryAttribute>();

                if (routeAttribute != null)
                {
                    var routeKey = routeAttribute.Name ?? parameterName;
                    if (routeKey != null && routeValues.ContainsKey(routeKey))
                    {
                        values[i] = ConvertValue(routeValues[routeKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (bodyAttribute != null)
                {
                    values[i] = await TransformInputBodyAsync(actionKey, context, parameter);
                }
                else if (headerAttribute != null)
                {
                    var headerKey = headerAttribute.Name ?? parameterName;
                    if (headerKey != null && context.Request.Headers.ContainsKey(headerKey))
                    {
                        values[i] = ConvertValue(context.Request.Headers[headerKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (queryAttribute != null)
                {
                    var queryKey = queryAttribute.Name ?? parameterName;
                    if (IsSimpleType(parameter.ParameterType))
                    {
                        if (queryKey != null && context.Request.Query.ContainsKey(queryKey))
                        {
                            values[i] = ConvertValue(context.Request.Query[queryKey].ToString(), parameter.ParameterType);
                        }
                    }
                    else
                    {
                        var bound = BindFromQuery(context.Request.Query, queryKey, parameter.ParameterType);
                        if (bound != null)
                            values[i] = bound;
                    }
                }
                else
                {
                    if (IsSimpleType(parameter.ParameterType))
                    {
                        if (parameterName != null && context.Request.Query.ContainsKey(parameterName))
                        {
                            values[i] = ConvertValue(context.Request.Query[parameterName].ToString(), parameter.ParameterType);
                        }
                    }
                    else
                    {
                        var bound = BindFromQuery(context.Request.Query, parameterName, parameter.ParameterType);
                        if (bound != null)
                            values[i] = bound;
                    }
                }

                // Fall back to the parameter's declared default value when no
                // binding source produced a value (supports optional params).
                if (values[i] == null && parameter.HasDefaultValue)
                {
                    values[i] = parameter.DefaultValue;
                }
            }

            return values;
        }

        private async Task<object?> TransformInputBodyAsync(string actionKey, HttpContext context, ParameterInfo parameter)
        {
            var jsonOptions = GetJsonOptions(context);
            var body = await ReadRequestBody(context);
            var transformManager = context.RequestServices.GetService<TransformManager>();

            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                if (transformManager != null)
                {
                    var version = context.Request.Query["api-version"].FirstOrDefault();
                    version = version ?? context.Request.Headers["x-api-version"].FirstOrDefault();
                    version = version ?? "1970-01-01";

                    TransformKey key = new(actionKey, version, TransformDirection.Input);
                    Type type = transformManager.GetFirstInputType(key) ?? parameter.ParameterType;
                    var apiInput = JsonSerializer.Deserialize(body, type, jsonOptions);

                    apiInput = await transformManager.TransformAsync(key, apiInput);
                    return apiInput;
                }

                return JsonSerializer.Deserialize(body, parameter.ParameterType, jsonOptions);
            }
            catch (JsonException jsonEx)
            {
                throw new BadHttpRequestException($"Invalid request body: {jsonEx.Message}", jsonEx);
            }
        }

        public async Task<object?> TransformOutputBodyAsync(string actionKey, HttpContext context, object? apiOutput)
        {
            var transformManager = context.RequestServices.GetService<TransformManager>();

            if (transformManager != null)
            {
                var version = context.Request.Query["api-version"].FirstOrDefault();
                version = version ?? context.Request.Headers["x-api-version"].FirstOrDefault();
                version = version ?? "1970-01-01";
                TransformKey key = new(actionKey, version, TransformDirection.Output);

                apiOutput = await transformManager.TransformAsync(key, apiOutput);
            }
            return apiOutput;
        }

        private object? ConvertValue(string? value, Type targetType)
        {
            if (value == null)
                return null;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying.IsEnum)
                return Enum.Parse(underlying, value, ignoreCase: true);

            if (underlying == typeof(string))
                return value;

            if (underlying == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);

            if (underlying == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);

            if (underlying == typeof(bool))
                return bool.Parse(value);

            if (underlying == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture);

            if (underlying == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

            if (underlying == typeof(Guid))
                return Guid.Parse(value);

            if (underlying == typeof(Id))
            {
                return new Id(value);
            }

            return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
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

        private async Task ProcessResult(string actionKey, HttpContext context, object? result, Type returnType)
        {
            var jsonOptions = GetJsonOptions(context);
            context.Response.ContentType = "application/json";

            if (returnType == typeof(void))
            {
                context.Response.StatusCode = 204;
                return;
            }

            if (result is Task task)
            {
                await task;

                if (returnType.IsGenericType)
                {
                    result = returnType.GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    // Non-generic Task (Task.CompletedTask-style) → No Content.
                    context.Response.StatusCode = 204;
                    return;
                }
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask;
                context.Response.StatusCode = 204;
                return;
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                // ValueTask<T> — extract the result via reflection.
                var resultProperty = returnType.GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(result);
                }
            }

            result = await TransformOutputBodyAsync(actionKey, context, result);

            if (result == null)
            {
                context.Response.StatusCode = 204;
                return;
            }

            var json = JsonSerializer.Serialize(result, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private bool IsSimpleType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsPrimitive)
                return true;
            if (t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid))
                return true;
            if (t.IsEnum)
                return true;
            if (t == typeof(Id))
                return true;
            return false;
        }

        private object? BindFromQuery(IQueryCollection query, string? prefix, Type targetType)
        {
            var hasPrefixed = prefix != null && HasAnyKeyForPrefix(query, prefix);
            if (!hasPrefixed)
            {
                var anyUnprefixed = HasAnyKeyForAnyProperty(query, targetType);
                if (!anyUnprefixed)
                    return null;
                var instanceUnprefixed = Activator.CreateInstance(targetType)!;
                BindProperties(query, null, instanceUnprefixed);
                return instanceUnprefixed;
            }

            var instance = Activator.CreateInstance(targetType)!;
            BindProperties(query, prefix, instance);
            return instance;
        }

        private bool HasAnyKeyForPrefix(IQueryCollection query, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return false;
            foreach (var key in query.Keys)
            {
                if (key.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (key.StartsWith(prefix + "[", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool HasAnyKeyForAnyProperty(IQueryCollection query, Type targetType)
        {
            foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite)
                    continue;
                var name = prop.Name;
                foreach (var key in query.Keys)
                {
                    if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (key.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (key.StartsWith(name + "[", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void BindProperties(IQueryCollection query, string? prefix, object instance)
        {
            var type = instance.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite)
                    continue;

                var propType = prop.PropertyType;
                var keyBase = string.IsNullOrWhiteSpace(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                var keyBaseBracket = string.IsNullOrWhiteSpace(prefix) ? prop.Name : $"{prefix}[{prop.Name}]";

                if (IsSimpleType(propType))
                {
                    if (query.ContainsKey(keyBase))
                    {
                        var v = query[keyBase].ToString();
                        var cv = ConvertValue(v, propType);
                        prop.SetValue(instance, cv);
                        continue;
                    }
                    if (query.ContainsKey(keyBaseBracket))
                    {
                        var v = query[keyBaseBracket].ToString();
                        var cv = ConvertValue(v, propType);
                        prop.SetValue(instance, cv);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(prefix) && query.ContainsKey(prop.Name))
                    {
                        var v = query[prop.Name].ToString();
                        var cv = ConvertValue(v, propType);
                        prop.SetValue(instance, cv);
                        continue;
                    }
                }
                else if (IsEnumerableType(propType, out var elemType))
                {
                    var values = new List<string>();
                    if (query.TryGetValue(keyBase, out var sv) && sv.Count > 0)
                        values.AddRange(sv.OfType<string>());
                    else if (query.TryGetValue(keyBaseBracket, out var svb) && svb.Count > 0)
                        values.AddRange(svb.OfType<string>());
                    else
                    {
                        var idxValues = new SortedDictionary<int, string>();
                        foreach (var k in query.Keys)
                        {
                            if (k.StartsWith(keyBase + "[", StringComparison.OrdinalIgnoreCase) && k.EndsWith("]"))
                            {
                                var inside = k.Substring(keyBase.Length + 1, k.Length - keyBase.Length - 2);
                                if (int.TryParse(inside, out var index))
                                {
                                    idxValues[index] = query[k].ToString();
                                }
                            }
                            if (k.StartsWith(keyBaseBracket + "[", StringComparison.OrdinalIgnoreCase) && k.EndsWith("]"))
                            {
                                var inside = k.Substring(keyBaseBracket.Length + 1, k.Length - keyBaseBracket.Length - 2);
                                if (int.TryParse(inside, out var index))
                                {
                                    idxValues[index] = query[k].ToString();
                                }
                            }
                        }
                        if (idxValues.Count > 0)
                            values.AddRange(idxValues.Values);
                    }

                    if (values.Count > 0)
                    {
                        if (IsSimpleType(elemType))
                        {
                            if (propType.IsArray)
                            {
                                var arr = Array.CreateInstance(elemType, values.Count);
                                for (int i = 0; i < values.Count; i++)
                                    arr.SetValue(ConvertValue(values[i], elemType), i);
                                prop.SetValue(instance, arr);
                            }
                            else
                            {
                                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
                                foreach (var s in values)
                                    list.Add(ConvertValue(s, elemType));
                                prop.SetValue(instance, list);
                            }
                        }
                        else
                        {
                            var complexList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
                            var count = values.Count;
                            for (int i = 0; i < count; i++)
                            {
                                var item = Activator.CreateInstance(elemType)!;
                                BindProperties(query, keyBase + $"[{i}]", item);
                                complexList.Add(item);
                            }
                            if (propType.IsArray)
                            {
                                var arr = Array.CreateInstance(elemType, complexList.Count);
                                for (int i = 0; i < complexList.Count; i++)
                                    arr.SetValue(complexList[i], i);
                                prop.SetValue(instance, arr);
                            }
                            else
                            {
                                prop.SetValue(instance, complexList);
                            }
                        }
                        continue;
                    }
                    else
                    {
                        if (!IsSimpleType(elemType))
                        {
                            var idx = 0;
                            var any = false;
                            while (true)
                            {
                                var nestedPrefix = keyBase + $"[{idx}]";
                                var nestedPrefixBracket = keyBaseBracket + $"[{idx}]";
                                if (HasAnyKeyForPrefix(query, nestedPrefix) || HasAnyKeyForPrefix(query, nestedPrefixBracket))
                                {
                                    any = true;
                                    idx++;
                                }
                                else break;
                            }
                            if (any)
                            {
                                var complexList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
                                for (int i = 0; i < idx; i++)
                                {
                                    var item = Activator.CreateInstance(elemType)!;
                                    BindProperties(query, keyBase + $"[{i}]", item);
                                    complexList.Add(item);
                                }
                                if (propType.IsArray)
                                {
                                    var arr = Array.CreateInstance(elemType, complexList.Count);
                                    for (int i = 0; i < complexList.Count; i++)
                                        arr.SetValue(complexList[i], i);
                                    prop.SetValue(instance, arr);
                                }
                                else
                                {
                                    prop.SetValue(instance, complexList);
                                }
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (HasAnyKeyForPrefix(query, keyBase) || HasAnyKeyForPrefix(query, keyBaseBracket))
                    {
                        var nested = Activator.CreateInstance(propType)!;
                        BindProperties(query, keyBase, nested);
                        prop.SetValue(instance, nested);
                        continue;
                    }
                }
            }
        }

        private bool IsEnumerableType(Type type, out Type elementType)
        {
            elementType = null!;
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                return true;
            }
            if (type.IsGenericType)
            {
                var gen = type.GetGenericTypeDefinition();
                if (gen == typeof(List<>) || gen == typeof(IList<>) || gen == typeof(IEnumerable<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }
            var iface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (iface != null)
            {
                elementType = iface.GetGenericArguments()[0];
                return true;
            }
            return false;
        }
    }
}
