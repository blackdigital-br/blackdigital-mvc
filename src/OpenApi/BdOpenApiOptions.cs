using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Configuration options for the REST-style OpenAPI document builder and
    /// its ASP.NET Core Scalar integration endpoints.
    /// </summary>
    public class BdOpenApiOptions
    {
        /// <summary>
        /// OpenAPI document <c>info.title</c>.
        /// </summary>
        public string Title { get; set; } = "API";

        /// <summary>
        /// OpenAPI document <c>info.version</c> (e.g. "v1").
        /// </summary>
        public string Version { get; set; } = "v1";

        /// <summary>
        /// Optional <c>info.description</c> shown under the title in the UI.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional <c>info.contact</c> metadata.
        /// </summary>
        public OpenApiContact? Contact { get; set; }

        /// <summary>
        /// Optional <c>info.license</c> metadata.
        /// </summary>
        public OpenApiLicense? License { get; set; }

        /// <summary>
        /// Optional server URL injected into <c>servers[]</c>. When null the
        /// builder derives it from the current HTTP request (scheme + host).
        /// </summary>
        public string? ServerUrl { get; set; }

        /// <summary>
        /// Short document name used in the route <c>/openapi/{documentName}.json</c>
        /// and as the default document id inside Scalar.
        /// </summary>
        public string DocumentName { get; set; } = "v1";

        /// <summary>
        /// When true the OpenAPI endpoints and Scalar UI are exposed. Defaults
        /// to true — it is the caller's responsibility to gate this in
        /// non-local environments (e.g.
        /// <c>options.Enabled = env.IsDevelopment() || env.IsStaging()</c>).
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Assembly name prefixes to scan for <c>[ServiceAttribute]</c>
        /// interfaces. When empty (default) every non-BCL assembly loaded in
        /// the current <see cref="AppDomain"/> is inspected.
        /// Example: <c>{ "MyProject", "Acme.Billing" }</c>.
        /// </summary>
        public HashSet<string> IncludedAssemblyPrefixes { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Assembly name prefixes that are always excluded from service
        /// discovery, regardless of <see cref="IncludedAssemblyPrefixes"/>.
        /// Defaults cover the common .NET BCL, runtime and third-party lib
        /// prefixes. You can add more values to narrow scanning further.
        /// </summary>
        public HashSet<string> ExcludedAssemblyPrefixes { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "System",
            "Microsoft",
            "mscorlib",
            "netstandard",
            "NuGet",
            "JetBrains",
            "xunit",
            "NUnit",
            "nunit",
            "Newtonsoft",
            "Swashbuckle",
            "Scalar",
            "Serilog",
            "Npgsql",
            "Pomelo",
            "MySql",
            "Oracle",
            "Dapper",
            "AutoMapper",
            "FluentValidation",
            "MediatR",
            "Polly",
            "IdentityServer",
            "OpenTelemetry",
            "Prometheus",
            "GraphQL",
            "MongoDB",
            "StackExchange",
            "BlackDigital.AspNet",
            "BlackDigital.Rest",
            "BlackDigital.Core",
            "BlackDigital.Data"
        };

        /// <summary>
        /// Security schemes registered under <c>components.securitySchemes</c>.
        /// A scheme keyed as <c>"Bearer"</c> with a standard JWT Bearer setup
        /// is added by default — overwrite or clear to opt out.
        /// </summary>
        public Dictionary<string, OpenApiSecurityScheme> SecuritySchemes { get; } = new()
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT Bearer token."
            }
        };

        /// <summary>
        /// When true, a <c>security: [{ "Bearer": [] }]</c> entry is added at
        /// the document level so every operation inherits Bearer auth by
        /// default. Defaults to true when <c>SecuritySchemes</c> contains a
        /// <c>"Bearer"</c> entry.
        /// </summary>
        public bool ApplyBearerAsGlobalSecurityRequirement { get; set; } = true;

        /// <summary>
        /// List of security scheme ids pre-selected in the Scalar UI auth
        /// panel. Defaults to <c>["Bearer"]</c>.
        /// </summary>
        public List<string> PreferredSecuritySchemes { get; } = new() { "Bearer" };

        /// <summary>
        /// Scalar document title visible in the Scalar UI browser tab. Defaults
        /// to <see cref="Title"/>.
        /// </summary>
        public string? ScalarTitle { get; set; }

        /// <summary>
        /// Route pattern used to expose the generated JSON. Must include the
        /// <c>{documentName}</c> route token so Scalar can resolve the
        /// <see cref="DocumentName"/>.
        /// </summary>
        public string OpenApiRoutePattern { get; set; } = "/openapi/{documentName}.json";

        /// <summary>
        /// Creates a shallow copy of this options instance. Collection-valued
        /// properties are copied into new collections so the clone can be
        /// mutated independently (used by the document provider to inject a
        /// request-derived <see cref="ServerUrl"/> without mutating the shared
        /// singleton options).
        /// </summary>
        public BdOpenApiOptions Clone()
        {
            var clone = new BdOpenApiOptions
            {
                Title = Title,
                Version = Version,
                Description = Description,
                Contact = Contact,
                License = License,
                ServerUrl = ServerUrl,
                DocumentName = DocumentName,
                Enabled = Enabled,
                ApplyBearerAsGlobalSecurityRequirement = ApplyBearerAsGlobalSecurityRequirement,
                ScalarTitle = ScalarTitle,
                OpenApiRoutePattern = OpenApiRoutePattern
            };

            foreach (var x in IncludedAssemblyPrefixes) clone.IncludedAssemblyPrefixes.Add(x);
            foreach (var x in ExcludedAssemblyPrefixes) clone.ExcludedAssemblyPrefixes.Add(x);
            // Deep-copy security schemes so mutating the clone's dictionary
            // entries (e.g. replacing a scheme) never affects the original.
            foreach (var kv in SecuritySchemes)
                clone.SecuritySchemes[kv.Key] = new OpenApiSecurityScheme
                {
                    Type = kv.Value.Type,
                    Scheme = kv.Value.Scheme,
                    BearerFormat = kv.Value.BearerFormat,
                    Description = kv.Value.Description,
                    Name = kv.Value.Name,
                    In = kv.Value.In,
                    OpenIdConnectUrl = kv.Value.OpenIdConnectUrl,
                    Flows = kv.Value.Flows
                };
            foreach (var x in PreferredSecuritySchemes) clone.PreferredSecuritySchemes.Add(x);

            return clone;
        }
    }

}
