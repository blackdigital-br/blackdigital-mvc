
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
        OpenApiDocument GetDocument(HttpContext context);

        /// <summary>Document name (e.g. "v1"). Used for endpoint routing.</summary>
        string DocumentName { get; }

        /// <summary>Writes the current document to <paramref name="stream"/> as JSON.</summary>
        void WriteAsJson(HttpContext context, Stream stream);
    }
}
