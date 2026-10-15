using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.Model;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Custom middleware that intercepts HTTP requests and routes them to
    /// services decorated with route and action attributes, providing automatic
    /// REST functionality. The middleware inspects registered services that
    /// carry <c>ServiceAttribute</c> and <c>ActionAttribute</c>, builds dynamic
    /// routes and executes the corresponding methods based on the request URL
    /// and HTTP method.
    /// </summary>
    public class RestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RestRouteRegistry _routeRegistry;
        private readonly ILogger<RestMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly RestOptions _options;
        private JsonSerializerOptions? _cachedJsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the request pipeline.</param>
        /// <param name="routeRegistry">The pre-built route table for REST services.</param>
        /// <param name="logger">The logger used for error reporting and request logging.</param>
        /// <param name="environment">The hosting environment used to control stack-trace exposure.</param>
        /// <param name="options">The REST middleware options controlling optional cross-cutting behavior.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public RestMiddleware(RequestDelegate next,
                              RestRouteRegistry routeRegistry,
                              ILogger<RestMiddleware> logger,
                              IWebHostEnvironment environment,
                              IOptions<RestOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _routeRegistry = routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _options = options?.Value ?? new RestOptions();
        }

        /// <summary>
        /// Processes the incoming HTTP request. When the request matches a
        /// registered REST route, the corresponding service method is invoked;
        /// otherwise the request is forwarded to the next pipeline component.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Attempts to match the request path and method against the registered
        /// route table, populating the route values from template segments.
        /// </summary>
        /// <param name="path">The lowercased request path.</param>
        /// <param name="httpMethod">The uppercased HTTP method.</param>
        /// <param name="routeEntry">The matched route entry, or null when no match is found.</param>
        /// <param name="routeValues">The dictionary populated with route template values.</param>
        /// <returns><c>true</c> when a matching route was found; otherwise, <c>false</c>.</returns>
        private bool TryMatchRoute(string path, string httpMethod, out RestRouteEntry? routeEntry, out Dictionary<string, object> routeValues)
        {
            routeEntry = null;
            routeValues = new Dictionary<string, object>();

            // Fast path: static routes (no template parameters) are looked up
            // directly from a dictionary keyed by the lowercased path. This
            // avoids iterating the route list and splitting strings for the
            // common case of fixed URLs.
            var staticRoutes = _routeRegistry.GetStaticRoutes(httpMethod);
            if (staticRoutes.TryGetValue(path, out var staticEntry))
            {
                routeEntry = staticEntry;
                return true;
            }

            // Slow path: parameterized routes. Split the request path once and
            // only consider routes with the same segment count.
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var parameterized = _routeRegistry.GetParameterizedRoutes(httpMethod);

            if (parameterized.TryGetValue(pathSegments.Length, out var candidates))
            {
                foreach (var entry in candidates)
                {
                    if (MatchesPattern(pathSegments, entry, routeValues))
                    {
                        routeEntry = entry;
                        return true;
                    }

                    routeValues.Clear();
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether a request path matches a route pattern, segment by
        /// segment. Template segments enclosed in braces (<c>{param}</c>) capture
        /// the corresponding path segment into <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="pathSegments">The request path already split into segments.</param>
        /// <param name="entry">The route entry whose pre-split pattern segments are compared.</param>
        /// <param name="routeValues">The dictionary to populate with captured values.</param>
        /// <returns><c>true</c> when the path matches the pattern; otherwise, <c>false</c>.</returns>
        private static bool MatchesPattern(string[] pathSegments, RestRouteEntry entry, Dictionary<string, object> routeValues)
        {
            var patternSegments = entry.Segments;

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

        /// <summary>
        /// Executes the matched REST route: resolves authorization, instantiates
        /// the service, binds method parameters from the request, invokes the
        /// action method, and serializes the result. Exceptions are caught and
        /// translated into HTTP responses. Request/response logging, rate
        /// limiting and response caching are applied here when configured.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="routeEntry">The matched route entry containing the service and method.</param>
        /// <param name="routeValues">The route template values captured from the URL.</param>
        private async Task HandleRestRequest(HttpContext context, RestRouteEntry routeEntry, Dictionary<string, object> routeValues)
        {
            var methodInfo = routeEntry.Method;
            var serviceDescriptor = routeEntry.Service;

            var stopwatch = _options.EnableRequestLogging ? Stopwatch.StartNew() : null;
            string? requestBody = null;

            try
            {
                // Verificar autorização
                var actionAttribute = methodInfo.GetCustomAttribute<ActionAttribute>();
                var serviceAttribute = serviceDescriptor.ServiceType.GetCustomAttribute<ServiceAttribute>();

                var actionKey = actionAttribute?.KeyIdentifier ?? $"{actionAttribute?.Method.ToString().ToUpperInvariant()}:{serviceAttribute?.BaseRoute}/{actionAttribute?.Route}";

                // Combine service-level policy prefix with action-level policy suffix.
                // Examples: service "Category" + action "Read" => "Category.Read"
                //           service "Category" + action null   => "Category"
                var requiredPolicy = CombinePolicies(serviceAttribute?.Policy, actionAttribute?.Policy);

                var requiresAuth = (actionAttribute?.Authorize == true)
                                || (serviceAttribute?.Authorize == true)
                                || !string.IsNullOrEmpty(requiredPolicy);

                if (requiresAuth)
                {
                    if (!context.User.Identity?.IsAuthenticated ?? true)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized");
                        return;
                    }

                    // If a policy is required, evaluate it through the standard
                    // ASP.NET Core authorization service.
                    if (!string.IsNullOrEmpty(requiredPolicy))
                    {
                        var authorizationService = context.RequestServices
                            .GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();

                        var authResult = await authorizationService.AuthorizeAsync(context.User, null, requiredPolicy);
                        if (!authResult.Succeeded)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Forbidden");
                            return;
                        }
                    }
                }

                // Rate limiting — applied after authorization so anonymous callers
                // cannot bypass it, but before the service is invoked.
                var rateLimitAttr = methodInfo.GetCustomAttribute<RateLimitAttribute>()
                                    ?? serviceDescriptor.ServiceType.GetCustomAttribute<RateLimitAttribute>();
                if (rateLimitAttr != null)
                {
                    if (!await CheckRateLimitAsync(context, actionKey, rateLimitAttr))
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        return;
                    }
                }

                // Capture the request body for logging before it is consumed.
                if (_options.EnableRequestLogging && _options.LogRequestBody)
                {
                    requestBody = await ReadRequestBody(context);
                }

                // Obter instância do serviço
                var service = context.RequestServices.GetService(serviceDescriptor.ServiceType);
                if (service == null)
                {
                    throw new InvalidOperationException($"Service not found to: {serviceDescriptor.ServiceType}");
                }

                // Preparar parâmetros — rent an array from the pool to avoid
                // allocating a new object[] on every request.
                var parameterCount = methodInfo.GetParameters().Length;
                var parameters = parameterCount > 0
                    ? System.Buffers.ArrayPool<object?>.Shared.Rent(parameterCount)
                    : Array.Empty<object?>();

                try
                {
                    await PrepareMethodParameters(actionKey, context, methodInfo, parameters, routeValues);

                    // Executar método
                    var result = MethodInvoker.Invoke(methodInfo, service, parameters);

                    // Aplicar headers de cache quando a action ou interface tiver [Cache].
                    var cacheAttr = methodInfo.GetCustomAttribute<CacheAttribute>();
                    if (cacheAttr != null)
                    {
                        ApplyCacheHeaders(context, cacheAttr);
                    }

                    // Processar resultado
                    await ProcessResult(actionKey, context, result, methodInfo.ReturnType);
                }
                finally
                {
                    if (parameters.Length > 0)
                        System.Buffers.ArrayPool<object?>.Shared.Return(parameters, clearArray: true);
                }
            }
            catch (BusinessException businessException)
            {
                context.Response.StatusCode = businessException.Code;
                await WriteProblemDetailsAsync(context, businessException.Code, "Business Error", businessException.Message);
            }
            catch (BadHttpRequestException badRequest)
            {
                context.Response.StatusCode = badRequest.StatusCode;
                await WriteProblemDetailsAsync(context, badRequest.StatusCode, "Bad Request", badRequest.Message);
            }
            catch (Exception ex)
            {
                // methodInfo.Invoke wraps the thrown exception in TargetInvocationException;
                // unwrap to the root business exception when present.
                var rootException = ex.GetBaseException();
                if (rootException is BusinessException innerBusinessException)
                {
                    context.Response.StatusCode = innerBusinessException.Code;
                    await WriteProblemDetailsAsync(context, innerBusinessException.Code, "Business Error", innerBusinessException.Message);
                }
                else
                {
                    _logger.LogError(ex, "Error on process Rest Service");
                    context.Response.StatusCode = 500;

                    // Never leak stack traces outside the development environment.
                    var includeStackTrace = _environment.IsDevelopment();
                    var problem = new ProblemDetails
                    {
                        Status = 500,
                        Title = "Internal Server Error",
                        Detail = rootException.Message
                    };

                    if (includeStackTrace && rootException.StackTrace != null)
                    {
                        problem.Extensions["stackTrace"] = rootException.StackTrace;
                    }

                    await WriteProblemDetailsAsync(context, problem);
                }
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    await LogRequestAsync(context, methodInfo, stopwatch.Elapsed, requestBody);
                }
            }
        }


        /// <summary>
        /// Resolves the JSON serializer options from the request services,
        /// falling back to the web defaults when none are registered. The
        /// resolved options are cached for the lifetime of the middleware
        /// instance to avoid repeated DI lookups and allocations on every
        /// request.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>The resolved <see cref="JsonSerializerOptions"/>.</returns>
        private JsonSerializerOptions GetJsonOptions(HttpContext context)
        {
            if (_cachedJsonOptions != null)
                return _cachedJsonOptions;

            _cachedJsonOptions = context.RequestServices
                                .GetService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                                ?.Value?.JsonSerializerOptions
                                ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

            return _cachedJsonOptions;
        }

        /// <summary>
        /// Builds the argument array for the action method by inspecting each
        /// parameter's binding attributes (<c>[Path]</c>, <c>[Body]</c>,
        /// <c>[Header]</c>, <c>[Query]</c>) and reading the corresponding value
        /// from the request. Parameters with no explicit attribute are bound
        /// from the query string when simple, or model-bound when complex.
        /// Unbound optional parameters fall back to their declared default.
        /// </summary>
        /// <param name="actionKey">The action key used to look up input transform rules.</param>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="methodInfo">The method to prepare parameters for.</param>
        /// <param name="values">The array to populate with parameter values (rented from ArrayPool by the caller).</param>
        /// <param name="routeValues">The route template values captured from the URL.</param>
        private async Task PrepareMethodParameters(string actionKey, HttpContext context, MethodInfo methodInfo,
                                                   object?[] values, Dictionary<string, object> routeValues)
        {
            var jsonOptions = GetJsonOptions(context);

            // Use cached parameter metadata to avoid per-request reflection
            // (GetParameters + GetCustomAttribute on every call).
            var parameters = ParameterMetadataCache.Get(methodInfo);

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                // CancellationToken is bound to the request's aborted token so
                // long-running service methods respect client disconnects.
                if (parameter.IsCancellationToken)
                {
                    values[i] = context.RequestAborted;
                    continue;
                }

                if (parameter.RouteAttribute != null)
                {
                    if (parameter.RouteKey != null && routeValues.ContainsKey(parameter.RouteKey))
                    {
                        values[i] = ConvertValue(routeValues[parameter.RouteKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (parameter.IsBody)
                {
                    // [Body] parameters of type IFormFile / IFormFileCollection
                    // are read from the multipart form files instead of being
                    // JSON-deserialized.
                    if (parameter.IsFormFile)
                    {
                        values[i] = context.Request.Form.Files.FirstOrDefault();
                    }
                    else if (parameter.IsFormFileCollection)
                    {
                        values[i] = context.Request.Form.Files;
                    }
                    else
                    {
                        values[i] = await TransformInputBodyAsync(actionKey, context, parameter);
                    }
                }
                else if (parameter.HeaderAttribute != null)
                {
                    if (parameter.HeaderKey != null && context.Request.Headers.ContainsKey(parameter.HeaderKey))
                    {
                        values[i] = ConvertValue(context.Request.Headers[parameter.HeaderKey].ToString(), parameter.ParameterType);
                    }
                }
                else if (parameter.QueryAttribute != null)
                {
                    if (parameter.IsSimpleType)
                    {
                        if (parameter.QueryKey != null && context.Request.Query.ContainsKey(parameter.QueryKey))
                        {
                            values[i] = ConvertValue(context.Request.Query[parameter.QueryKey].ToString(), parameter.ParameterType);
                        }
                    }
                    else
                    {
                        var bound = BindFromQuery(context.Request.Query, parameter.QueryKey, parameter.ParameterType);
                        if (bound != null)
                            values[i] = bound;
                    }
                }
                else
                {
                    if (parameter.IsSimpleType)
                    {
                        if (parameter.Name != null && context.Request.Query.ContainsKey(parameter.Name))
                        {
                            values[i] = ConvertValue(context.Request.Query[parameter.Name].ToString(), parameter.ParameterType);
                        }
                    }
                    else
                    {
                        var bound = BindFromQuery(context.Request.Query, parameter.Name, parameter.ParameterType);
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
        }

        /// <summary>
        /// Reads and deserializes the request body, applying any registered
        /// input transform rules for the action. JSON parsing errors are
        /// rethrown as <see cref="BadHttpRequestException"/>.
        /// </summary>
        /// <param name="actionKey">The action key used to look up input transform rules.</param>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="parameter">The target parameter whose type defines the deserialization target.</param>
        /// <returns>The deserialized (and optionally transformed) body object, or null when the body is empty.</returns>
        private async Task<object?> TransformInputBodyAsync(string actionKey, HttpContext context, ParameterMetadata parameter)
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
                    version = version ?? string.Empty;

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

        /// <summary>
        /// Applies any registered output transform rules to the given value,
        /// resolving the API version from the query string or headers.
        /// </summary>
        /// <param name="actionKey">The action key identifying which rules to apply.</param>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="apiOutput">The value to transform.</param>
        /// <returns>A task that yields the transformed value, or the original value when no rules match.</returns>
        public async Task<object?> TransformOutputBodyAsync(string actionKey, HttpContext context, object? apiOutput)
        {
            var transformManager = context.RequestServices.GetService<TransformManager>();

            if (transformManager != null)
            {
                var version = context.Request.Query["api-version"].FirstOrDefault();
                version = version ?? context.Request.Headers["x-api-version"].FirstOrDefault();
                version = version ?? string.Empty;
                TransformKey key = new(actionKey, version, TransformDirection.Output);

                apiOutput = await transformManager.TransformAsync(key, apiOutput);
            }
            return apiOutput;
        }

        /// <summary>
        /// Converts a string value from a route, query string, or header into
        /// the target parameter type. Supports enums, common primitives,
        /// <see cref="DateTime"/>, <see cref="Guid"/>, the framework <see cref="Id"/>
        /// type, and falls back to <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/>.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <param name="targetType">The type to convert to (nullable underlying types are unwrapped).</param>
        /// <returns>The converted value, or null when the input is null.</returns>
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

        /// <summary>
        /// Reads the full request body as a string, enabling request body
        /// buffering so the stream can be read again later (e.g. by model
        /// binding or logging middleware).
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>The request body as a string.</returns>
        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            context.Request.Body.Position = 0;
            return body;
        }

        /// <summary>
        /// Writes a <see cref="ProblemDetails"/> response body in the format
        /// defined by RFC 7807. The response content type is set to
        /// <c>application/problem+json</c> as recommended by the RFC.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="problem">The problem details to serialize.</param>
        private async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problem)
        {
            context.Response.ContentType = "application/problem+json";
            var json = JsonSerializer.Serialize(problem, GetJsonOptions(context));
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Writes a <see cref="ProblemDetails"/> response with the given status,
        /// title and detail. Convenience overload for the common error paths.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="status">The HTTP status code.</param>
        /// <param name="title">A short, human-readable summary of the problem.</param>
        /// <param name="detail">A human-readable explanation specific to this occurrence.</param>
        private Task WriteProblemDetailsAsync(HttpContext context, int status, string title, string detail)
            => WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            });

        /// <summary>
        /// Processes the result of an action method invocation. Awaits
        /// <see cref="Task"/>/<see cref="ValueTask"/> returns, applies output
        /// transform rules, and writes the JSON response. Null or void results
        /// produce a 204 No Content response.
        /// </summary>
        /// <param name="actionKey">The action key used to look up output transform rules.</param>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="result">The value returned by the action method.</param>
        /// <param name="returnType">The declared return type of the action method.</param>
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
                    result = MethodInvoker.GetTaskResult(task, returnType);
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
                // ValueTask<T> — extract the result via cached compiled expression.
                result = MethodInvoker.GetValueTaskResult(result!, returnType);
            }

            result = await TransformOutputBodyAsync(actionKey, context, result);

            if (result == null)
            {
                context.Response.StatusCode = 204;
                return;
            }

            // IAsyncEnumerable<T> results are streamed to the response body
            // without materializing the whole collection in memory.
            if (IsAsyncEnumerable(result, out var asyncEnumerable, out var elementType))
            {
                await MethodInvoker.SerializeAsyncEnumerableAsync(context.Response.Body, asyncEnumerable, elementType, jsonOptions);
                return;
            }

            var json = JsonSerializer.Serialize(result, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Determines whether the given result implements
        /// <see cref="IAsyncEnumerable{T}"/> and, if so, returns the strongly
        /// typed enumerable and its element type for streaming serialization.
        /// </summary>
        /// <param name="result">The action method result to inspect.</param>
        /// <param name="asyncEnumerable">The result cast to <see cref="IAsyncEnumerable{T}"/>, or null.</param>
        /// <param name="elementType">The element type of the asynchronous sequence.</param>
        /// <returns><c>true</c> when the result is an <see cref="IAsyncEnumerable{T}"/>; otherwise, <c>false</c>.</returns>
        private static bool IsAsyncEnumerable(object result, out object asyncEnumerable, out Type elementType)
        {
            asyncEnumerable = null!;
            elementType = null!;

            var iface = result.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

            if (iface == null)
                return false;

            asyncEnumerable = result;
            elementType = iface.GetGenericArguments()[0];
            return true;
        }

        /// <summary>
        /// Applies a <c>Cache-Control</c> response header derived from a
        /// <see cref="CacheAttribute"/>. Supports <c>public</c>/<c>private</c>
        /// scope, <c>max-age</c> and the <c>no-store</c> directive.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="cache">The cache attribute describing the desired caching behavior.</param>
        private static void ApplyCacheHeaders(HttpContext context, CacheAttribute cache)
        {
            var scope = cache.Location == CacheLocation.Client ? "private" : "public";

            if (cache.NoStore)
            {
                context.Response.Headers["Cache-Control"] = "no-store";
                return;
            }

            context.Response.Headers["Cache-Control"] = $"{scope}, max-age={cache.Duration}";
        }

        /// <summary>
        /// Evaluates the configured rate limit for the current action and client.
        /// The counter is stored in <see cref="IMemoryCache"/> keyed by action
        /// and client identifier, and resets after the configured window.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="actionKey">The action key used as part of the cache key.</param>
        /// <param name="rateLimit">The rate limit configuration.</param>
        /// <returns><c>true</c> when the request is within the limit; otherwise, <c>false</c>.</returns>
        private static async Task<bool> CheckRateLimitAsync(HttpContext context, string actionKey, RateLimitAttribute rateLimit)
        {
            var cache = context.RequestServices.GetService<IMemoryCache>();
            if (cache == null)
                return true;

            var clientId = GetClientIdentifier(context);
            var cacheKey = $"rl:{actionKey}:{clientId}";
            var window = TimeSpan.FromSeconds(rateLimit.WindowSeconds);

            var counter = await cache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return Task.FromResult(0);
            });

            if (counter >= rateLimit.Limit)
                return false;

            cache.Set(cacheKey, counter + 1, window);
            return true;
        }

        /// <summary>
        /// Resolves a stable identifier for the current client for rate-limit
        /// accounting. Prefers the authenticated user's name identifier claim;
        /// falls back to the remote IP address.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A client identifier string.</returns>
        private static string GetClientIdentifier(HttpContext context)
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                return userId;

            return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        }

        /// <summary>
        /// Writes a structured log entry for the current request when request
        /// logging is enabled. Includes HTTP method, path, status code,
        /// duration, and optionally the request/response bodies.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="methodInfo">The action method that handled the request.</param>
        /// <param name="elapsed">The time spent processing the request.</param>
        /// <param name="requestBody">The captured request body, or null.</param>
        private async Task LogRequestAsync(HttpContext context, MethodInfo methodInfo, TimeSpan elapsed, string? requestBody)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value;
            var statusCode = context.Response.StatusCode;
            var operation = methodInfo.DeclaringType?.Name + "." + methodInfo.Name;

            if (_options.LogResponseBody)
            {
                // The response body has already been written; we only log the
                // status code and content length to avoid buffering overhead.
                _logger.LogInformation(
                    "REST {Method} {Path} -> {StatusCode} ({ElapsedMs}ms) [{Operation}] reqBody={RequestBody}",
                    method, path, statusCode, elapsed.TotalMilliseconds, operation, requestBody ?? "<none>");
            }
            else
            {
                _logger.LogInformation(
                    "REST {Method} {Path} -> {StatusCode} ({ElapsedMs}ms) [{Operation}]",
                    method, path, statusCode, elapsed.TotalMilliseconds, operation);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Combines a service-level policy prefix with an action-level policy suffix.
        /// When both are present, they are joined with a dot (e.g. <c>Category</c> + <c>Read</c> = <c>Category.Read</c>).
        /// When only one is present, that value is returned. When neither is present, null is returned.
        /// </summary>
        /// <param name="servicePolicy">The service-level policy prefix.</param>
        /// <param name="actionPolicy">The action-level policy suffix.</param>
        /// <returns>The combined policy name, or null if both are null/empty.</returns>
        private static string? CombinePolicies(string? servicePolicy, string? actionPolicy)
        {
            var hasService = !string.IsNullOrWhiteSpace(servicePolicy);
            var hasAction = !string.IsNullOrWhiteSpace(actionPolicy);

            if (hasService && hasAction)
                return $"{servicePolicy}.{actionPolicy}";
            if (hasService)
                return servicePolicy!;
            if (hasAction)
                return actionPolicy!;
            return null;
        }

        /// <summary>
        /// Determines whether a type is a "simple" value type that can be
        /// converted from a string via <see cref="ConvertValue"/>: primitives,
        /// <see cref="string"/>, <see cref="decimal"/>, date/time types,
        /// <see cref="Guid"/>, enums, and the framework <see cref="Id"/> type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> when the type is simple; otherwise, <c>false</c>.</returns>
        private bool IsSimpleType(Type type)
            => RestParameterTypeCache.IsSimpleType(type);

        /// <summary>
        /// Binds a complex object from the query string. When the query contains
        /// keys prefixed with <paramref name="prefix"/>, properties are bound
        /// using that prefix; otherwise it falls back to binding from
        /// unprefixed property names. Returns null when no matching keys exist.
        /// </summary>
        /// <param name="query">The request query collection.</param>
        /// <param name="prefix">The optional prefix used to namespace the object's properties.</param>
        /// <param name="targetType">The type of object to create and bind.</param>
        /// <returns>A populated instance of <paramref name="targetType"/>, or null when no keys match.</returns>
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

        /// <summary>
        /// Checks whether the query string contains any key that equals
        /// <paramref name="prefix"/> or starts with <c>prefix.</c> or
        /// <c>prefix[</c> (used to detect whether a prefixed object is present).
        /// </summary>
        /// <param name="query">The request query collection.</param>
        /// <param name="prefix">The prefix to look for.</param>
        /// <returns><c>true</c> when at least one key matches the prefix; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Checks whether the query string contains any key that matches a
        /// public writable property of <paramref name="targetType"/> (by name,
        /// or with <c>.</c>/<c>[</c> suffixes). Used to decide whether to
        /// attempt unprefixed model binding.
        /// </summary>
        /// <param name="query">The request query collection.</param>
        /// <param name="targetType">The type whose properties are matched against query keys.</param>
        /// <returns><c>true</c> when at least one property has a matching query key; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Recursively binds the public writable properties of <paramref name="instance"/>
        /// from the query string. Simple properties are converted from matching
        /// keys; enumerable properties are collected from indexed or comma-separated
        /// keys; nested complex properties are bound recursively with a dotted or
        /// bracketed prefix.
        /// </summary>
        /// <param name="query">The request query collection.</param>
        /// <param name="prefix">The optional prefix to namespace property keys (null for root-level binding).</param>
        /// <param name="instance">The object whose properties are populated.</param>
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

        /// <summary>
        /// Determines whether a type is an enumerable collection and, if so,
        /// returns its element type. Supports arrays and the common generic
        /// collection interfaces (<see cref="List{T}"/>, <see cref="IList{T}"/>,
        /// <see cref="IEnumerable{T}"/>).
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="elementType">The element type of the collection, or null when the type is not enumerable.</param>
        /// <returns><c>true</c> when the type is an enumerable collection; otherwise, <c>false</c>.</returns>
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
