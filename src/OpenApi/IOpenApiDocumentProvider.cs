
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Provides a lazily-built and cached OpenAPI document. Implemented directly
    /// with Microsoft.OpenApi (no Swashbuckle, no Microsoft.AspNetCore.OpenApi)
    /// so callers can pin arbitrary Microsoft.OpenApi versions without NU1608
    /// warnings.
    /// </summary>
    public interface IOpenApiDocumentProvider
    {
        /// <summary>Returns the cached document, building it on first call.</summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>The built or cached <see cref="OpenApiDocument"/>.</returns>
        OpenApiDocument GetDocument(HttpContext context);

        /// <summary>Document name (e.g. "v1"). Used for endpoint routing.</summary>
        string DocumentName { get; }

        /// <summary>Writes the current document to <paramref name="stream"/> as JSON.</summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="stream">The destination stream to write the JSON document to.</param>
        void WriteAsJson(HttpContext context, Stream stream);
    }
}
