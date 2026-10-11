
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Extension methods for registering the OpenAPI document provider and
    /// mapping the OpenAPI JSON endpoint and Scalar UI into the request pipeline.
    /// </summary>
    public static class OpenApiEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="BdOpenApiOptions"/> (with the provided
        /// configuration callback) and the
        /// <see cref="IOpenApiDocumentProvider"/> service into DI.
        /// </summary>
        /// <remarks>
        /// Unlike the previous version, the Add/Map helpers do NOT filter by
        /// environment themselves — that decision belongs to the caller (for
        /// example by setting <c>options.Enabled = env.IsDevelopment()</c>
        /// from the host).
        /// </remarks>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configure">An optional callback to configure the OpenAPI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenApi(
            this IServiceCollection services,
            Action<BdOpenApiOptions>? configure = null)
        {
            services.Configure<BdOpenApiOptions>(options =>
            {
                if (configure != null) configure(options);
            });
            services.AddSingleton<IOpenApiDocumentProvider, OpenApiDocumentProvider>();
            return services;
        }

        /// <summary>
        /// Maps <c>/openapi/{documentName}.json</c> and the Scalar UI.
        /// Resolves all settings at runtime from
        /// <see cref="IOptions{BdOpenApiOptions}"/> so previously
        /// hardcoded route patterns, titles and Bearer preferences can be
        /// adjusted per host without touching this loader.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapOpenApi(
            this IEndpointRouteBuilder endpoints)
        {
            // Resolve options once via the application services.
            var optionsSnapshot = endpoints.ServiceProvider.GetRequiredService<IOptions<BdOpenApiOptions>>();
            var options = optionsSnapshot.Value;

            if (!options.Enabled)
                return endpoints;

            var documentName = string.IsNullOrWhiteSpace(options.DocumentName)
                ? "v1"
                : options.DocumentName;

            var routePattern = string.IsNullOrWhiteSpace(options.OpenApiRoutePattern)
                ? "/openapi/{documentName}.json"
                : options.OpenApiRoutePattern;

            var scalarTitle = string.IsNullOrWhiteSpace(options.ScalarTitle)
                ? options.Title
                : options.ScalarTitle;

            var preferredSchemes = options.PreferredSecuritySchemes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // --- Custom endpoint that serves our pure-Microsoft.OpenApi document ---
            endpoints.MapGet(routePattern, async (
                HttpContext context,
                string documentName,
                IOpenApiDocumentProvider provider) =>
            {
                if (!string.Equals(documentName, provider.DocumentName, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";

                await using var ms = new MemoryStream();
                provider.WriteAsJson(context, ms);
                ms.Position = 0;
                context.Response.ContentLength = ms.Length;
                await ms.CopyToAsync(context.Response.Body, context.RequestAborted);
            })
            .ExcludeFromDescription()
            .WithDisplayName("OpenAPI JSON Spec")
            .AllowAnonymous();

            // --- Scalar UI pointing to our custom JSON endpoint -------------------
            // Since we removed Microsoft.AspNetCore.OpenApi, Scalar cannot
            // auto-discover documents. We register them explicitly using the
            // options-driven values.
            endpoints.MapScalarApiReference(scalar =>
            {
                scalar.Title = scalarTitle;
                scalar.AddDocument(
                    documentName: documentName,
                    title: $"{scalarTitle} {options.Version}",
                    routePattern: routePattern,
                    isDefault: true,
                    agentKey: string.Empty);


                scalar.AddPreferredSecuritySchemes(preferredSchemes);
            });

            return endpoints;
        }
    }
}
