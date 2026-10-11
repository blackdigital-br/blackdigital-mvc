using System.Reflection;
using BlackDigital.Rest;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Builds a complete OpenAPI document for BlackDigital.Rest-based service
    /// interfaces using pure Microsoft.OpenApi types — no dependency on
    /// Microsoft.AspNetCore.OpenApi / Swashbuckle / third-party Swagger
    /// generators. It discovers service interfaces at runtime using the
    /// BlackDigital.Rest attributes and emits schemas via
    /// <see cref="BdOpenApiSchemaGenerator"/>.
    /// </summary>
    /// <remarks>
    /// All previously hardcoded "MyProject" strings (title, assembly filter,
    /// Bearer auth) have been moved into <see cref="BdOpenApiOptions"/> so
    /// this builder can be reused across any BlackDigital.AspNet-based project.
    /// </remarks>
    public class BdOpenApiDocumentBuilder
    {

        private readonly BdOpenApiOptions _options;
        private readonly BdOpenApiSchemaGenerator _schemas = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BdOpenApiDocumentBuilder"/> class.
        /// </summary>
        /// <param name="options">The options used to build the OpenAPI document.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public BdOpenApiDocumentBuilder(BdOpenApiOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the document title from the configured options.
        /// </summary>
        public string Title => _options.Title;

        /// <summary>
        /// Gets the document version from the configured options.
        /// </summary>
        public string Version => _options.Version;

        /// <summary>
        /// Gets the server URL from the configured options, or <c>null</c> when
        /// not set.
        /// </summary>
        public string? ServerUrl => _options.ServerUrl;

        /// <summary>
        /// Builds a complete <see cref="OpenApiDocument"/> by scanning the
        /// application domain for service interfaces decorated with
        /// <c>ServiceAttribute</c> and generating paths, operations and schemas.
        /// </summary>
        /// <param name="ct">A cancellation token for the build operation.</param>
        /// <returns>The generated OpenAPI document.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
        public OpenApiDocument Build(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = _options.Title,
                    Version = _options.Version,
                    Description = _options.Description,
                    Contact = _options.Contact,
                    License = _options.License
                }
            };

            // Server (optional caller-provided; fallback to path-relative refs).
            if (!string.IsNullOrEmpty(_options.ServerUrl))
            {
                var servers = new List<OpenApiServer>();
                servers.Add(new OpenApiServer { Url = _options.ServerUrl });
                document.Servers = servers;
            }

            // --- Components (always initialize so later sections can add to it
            //     safely, even when no security schemes are configured) ---------
            if (document.Components == null)
                document.Components = new OpenApiComponents();

            // --- Security schemes (from options, not hardcoded) ----------------
            OpenApiSecuritySchemeReference? bearerRef = null;
            if (_options.SecuritySchemes.Count > 0)
            {
                if (document.Components.SecuritySchemes == null)
                    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();

                foreach (var kvp in _options.SecuritySchemes)
                    document.Components.SecuritySchemes[kvp.Key] = kvp.Value;

                if (_options.SecuritySchemes.ContainsKey("Bearer"))
                    bearerRef = new OpenApiSecuritySchemeReference("Bearer", document);

                if (_options.ApplyBearerAsGlobalSecurityRequirement && bearerRef != null)
                {
                    if (document.Security == null)
                        document.Security = new List<OpenApiSecurityRequirement>();
                    var globalReq = new OpenApiSecurityRequirement();
                    globalReq[bearerRef] = new List<string>();
                    document.Security.Add(globalReq);
                }
            }

            // --- Dynamic service endpoints from BlackDigital interfaces --------
            var assemblies = GetCandidateAssemblies();

            var serviceInterfaces = assemblies
                .SelectMany(SafeGetTypes)
                .Where(t => t.IsInterface && t.GetCustomAttribute<ServiceAttribute>() != null)
                .ToList();

            if (document.Paths == null)
                document.Paths = new OpenApiPaths();

            // Register tag metadata per interface (drives the Scalar sidebar groups).
            // Stored as OpenApiTag on the document level; operations reference it
            // via OpenApiTagReference to keep the grouped order deterministic.
            var tagsByInterface = new Dictionary<Type, (OpenApiTag tag, OpenApiTagReference reference)>();
            foreach (var iface in serviceInterfaces)
            {
                var serviceAttr = iface.GetCustomAttribute<ServiceAttribute>()!;

                var tagName = !string.IsNullOrWhiteSpace(serviceAttr.GetServiceName())
                    ? serviceAttr.GetServiceName()
                    : FriendlyGroupName(iface);

                var tagDescription = !string.IsNullOrWhiteSpace(serviceAttr.GetServiceDescription())
                    ? serviceAttr.GetServiceDescription()
                    : $"Endpoints exposed by {iface.FullName ?? iface.Name}.";

                var tag = new OpenApiTag
                {
                    Name = tagName,
                    Description = tagDescription
                };
                var reference = new OpenApiTagReference(tag.Name, document);
                tagsByInterface[iface] = (tag, reference);
            }

            if (document.Tags == null)
                document.Tags = new HashSet<OpenApiTag>();
            foreach (var iface in serviceInterfaces)
                document.Tags.Add(tagsByInterface[iface].tag);

            foreach (var iface in serviceInterfaces)
            {
                var serviceAttr = iface.GetCustomAttribute<ServiceAttribute>()!;
                var baseRoute = ServiceAttributeHelper.GetRoute(serviceAttr).Trim('/');
                var (_, serviceTagRef) = tagsByInterface[iface];

                foreach (var method in iface.GetMethods())
                {
                    var actionAttr = method.GetCustomAttribute<ActionAttribute>();
                    if (actionAttr == null) continue;

                    var actionRoute = ActionAttributeHelper.GetRoute(actionAttr).Trim('/');
                    var opHttpMethod = ActionAttributeHelper.GetOperationType(actionAttr);
                    // Authorization is required when declared at the action level OR
                    // inherited from the service-level [ServiceAttribute], matching
                    // the runtime check in RestMiddleware.
                    var actionRequiresAuth = ActionAttributeHelper.GetAuthorize(actionAttr);
                    var serviceRequiresAuth = serviceAttr?.Authorize == true;
                    var requiresAuth = actionRequiresAuth || serviceRequiresAuth;

                    var fullRoute = "/" + string.Join("/",
                        new[] { baseRoute, actionRoute }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (!document.Paths.TryGetValue(fullRoute, out var rawPathItem))
                    {
                        rawPathItem = new OpenApiPathItem();
                        document.Paths[fullRoute] = rawPathItem;
                    }
                    var pathItem = (OpenApiPathItem)rawPathItem;

                    var operation = new OpenApiOperation
                    {
                        Summary = FormatMethodName(method.Name),
                        OperationId = method.DeclaringType?.Name + "_" + method.Name,
                        Responses = new OpenApiResponses(),
                        Tags = new HashSet<OpenApiTagReference> { serviceTagRef }
                    };

                    if (requiresAuth && bearerRef != null)
                    {
                        var opReq = new OpenApiSecurityRequirement();
                        opReq[bearerRef] = new List<string>();
                        operation.Security = new List<OpenApiSecurityRequirement> { opReq };
                    }

                    OpenApiRequestBody? requestBody = null;
                    foreach (var param in method.GetParameters())
                    {
                        var isBody = param.GetCustomAttribute<BodyAttribute>() != null;
                        var isPath = param.GetCustomAttribute<PathAttribute>() != null;
                        var isQuery = param.GetCustomAttribute<QueryAttribute>() != null;

                        if (isBody)
                        {
                            var schema = _schemas.GetSchema(param.ParameterType);
                            IDictionary<string, IOpenApiMediaType> content = new Dictionary<string, IOpenApiMediaType>();
                            content["application/json"] = new OpenApiMediaType { Schema = schema };
                            requestBody = new OpenApiRequestBody { Content = content };
                        }
                        else if (isPath)
                        {
                            var schema = _schemas.GetSchema(param.ParameterType);
                            if (operation.Parameters == null)
                                operation.Parameters = new List<IOpenApiParameter>();
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = param.Name ?? "id",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = schema
                            });
                        }
                        else if (isQuery)
                        {
                            // Expand property-by-property (classic OpenAPI style for complex query args).
                            var props = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            if (operation.Parameters == null)
                                operation.Parameters = new List<IOpenApiParameter>();
                            foreach (var prop in props)
                            {
                                var schema = _schemas.GetSchema(prop.PropertyType);
                                operation.Parameters.Add(new OpenApiParameter
                                {
                                    Name = prop.Name,
                                    In = ParameterLocation.Query,
                                    Required = false,
                                    Schema = schema
                                });
                            }
                        }
                    }

                    if (requestBody != null)
                        operation.RequestBody = requestBody;

                    var returnType = method.ReturnType;
                    Type? resultType = null;
                    if (returnType.IsGenericType)
                    {
                        var genDef = returnType.GetGenericTypeDefinition();
                        if (genDef == typeof(Task<>) || genDef == typeof(ValueTask<>))
                            resultType = returnType.GetGenericArguments()[0];
                        else
                            resultType = returnType;
                    }
                    else if (returnType == typeof(Task) || returnType == typeof(ValueTask))
                        resultType = null;
                    else
                        resultType = returnType;

                    if (resultType == null)
                    {
                        operation.Responses["204"] = new OpenApiResponse { Description = "No Content" };
                    }
                    else
                    {
                        var schema = _schemas.GetSchema(resultType);
                        IDictionary<string, IOpenApiMediaType> content = new Dictionary<string, IOpenApiMediaType>();
                        content["application/json"] = new OpenApiMediaType { Schema = schema };
                        operation.Responses["200"] = new OpenApiResponse
                        {
                            Description = "Success",
                            Content = content
                        };
                    }

                    // Use typed AddOperation helper (keyed by HttpMethod).
                    pathItem.AddOperation(opHttpMethod, operation);
                }
            }

            // --- Flush generated component schemas into the document ---------
            var components = _schemas.GetComponentSchemas();
            if (components.Count > 0)
            {
                if (document.Components.Schemas == null)
                    document.Components.Schemas = new Dictionary<string, IOpenApiSchema>();
                foreach (var kvp in components)
                    document.Components.Schemas[kvp.Key] = kvp.Value;
            }

            return document;
        }

        private static Type[] SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray()!; }
            catch { return Array.Empty<Type>(); }
        }

        /// <summary>
        /// Returns the assemblies that should be scanned for
        /// <see cref="ServiceAttribute"/> interfaces. Implements the
        /// options-driven include/exclude prefix policy that replaces the
        /// previous hardcoded <c>StartsWith("MyProject")</c> filter.
        /// </summary>
        private IEnumerable<Assembly> GetCandidateAssemblies()
        {
            var included = _options.IncludedAssemblyPrefixes;
            var excluded = _options.ExcludedAssemblyPrefixes;
            var hasExplicitInclude = included.Count > 0;

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = a.GetName().Name;
                if (string.IsNullOrEmpty(name)) continue;

                // Always kick out anything matching the BCL/vendor exclusion list.
                var isExcluded = false;
                foreach (var prefix in excluded)
                {
                    if (prefix.Length == 0) continue;
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        isExcluded = true;
                        break;
                    }
                }
                if (isExcluded) continue;

                // Dynamic assemblies can't be reflected for types reliably.
                if (a.IsDynamic) continue;
                // Skip assemblies with no public surface at all.
                if (string.Equals(a.GetName().Name, "Anonymously Hosted DynamicMethods Assembly", StringComparison.Ordinal))
                    continue;

                if (hasExplicitInclude)
                {
                    var match = false;
                    foreach (var prefix in included)
                    {
                        if (prefix.Length == 0) continue;
                        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match) continue;
                }

                yield return a;
            }
        }

        /// <summary>
        /// Turns a C# method name in PascalCase into a human-readable phrase.
        /// Examples:
        ///   GetCategoriesAsync       → "Get Categories"
        ///   GetCategoryByIdAsync     → "Get Category By Id"
        ///   ExpireOldInvitationsAsync → "Expire Old Invitations"
        /// </summary>
        private static string FormatMethodName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            // Drop the common TAP suffix.
            if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5)
                name = name.Substring(0, name.Length - 5);

            if (name.Length == 0)
                return name;

            // Split on each transition to an uppercase letter or digit boundary.
            // Also preserves sequences of uppercase letters (e.g. "ById" stays as
            // two words "By" "Id", while "APIKey" splits into "API Key", etc.).
            var sb = new System.Text.StringBuilder(capacity: name.Length + 8);
            sb.Append(name[0]);

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                var prev = name[i - 1];

                var splitHere = char.IsUpper(c)
                    || (char.IsDigit(c) && !char.IsDigit(prev))
                    || (!char.IsDigit(c) && char.IsDigit(prev));

                // Avoid double-space if a split was already emitted right before.
                if (splitHere && sb[^1] != ' ')
                    sb.Append(' ');

                sb.Append(c);
            }

            var result = sb.ToString();
            // Collapse any accidental double spaces.
            while (result.IndexOf("  ", StringComparison.Ordinal) >= 0)
                result = result.Replace("  ", " ", StringComparison.Ordinal);
            return result.Trim();
        }

        /// <summary>
        /// Turns a BlackDigital service interface name like IInvitationService
        /// into a human-friendly group name like "Invitations", with proper
        /// irregular English plurals for common suffixes.
        /// </summary>
        private static string FriendlyGroupName(Type serviceInterface)
        {
            var name = serviceInterface.Name;
            if (name.StartsWith("I", StringComparison.Ordinal) && name.Length > 1 && char.IsUpper(name[1]))
                name = name.Substring(1);

            if (name.EndsWith("AppService", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "AppService".Length);
            else if (name.EndsWith("Service", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Service".Length);

            if (string.IsNullOrWhiteSpace(name))
                name = serviceInterface.Name;

            // Pluralise with common English rules.
            var last = name.Length > 0 ? name[^1] : '\0';
            if (last == 's' || last == 'x' || last == 'z' ||
                name.EndsWith("ch", StringComparison.Ordinal) ||
                name.EndsWith("sh", StringComparison.Ordinal))
            {
                name += "es";
            }
            else if (last == 'y' && name.Length >= 2 && !IsVowel(name[^2]))
            {
                // "Category" → "Categories", "Policy" → "Policies" etc.
                name = name.Substring(0, name.Length - 1) + "ies";
            }
            else if (last != 's')
            {
                name += "s";
            }

            return name;
        }

        private static bool IsVowel(char c)
            => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'A' or 'E' or 'I' or 'O' or 'U';
    }
}
