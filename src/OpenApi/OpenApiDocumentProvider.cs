using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Default implementation of <see cref="IOpenApiDocumentProvider"/> that
    /// builds and caches the OpenAPI document per request scheme+host.
    /// </summary>
    public sealed class OpenApiDocumentProvider : IOpenApiDocumentProvider
    {
        private readonly object _sync = new();
        private readonly BdOpenApiOptions _options;
        private OpenApiDocument? _cached;
        private string? _cachedForSchemeHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiDocumentProvider"/> class.
        /// </summary>
        /// <param name="options">The options used to configure the OpenAPI document.</param>
        public OpenApiDocumentProvider(IOptions<BdOpenApiOptions> options)
        {
            _options = options.Value ?? new BdOpenApiOptions();
            DocumentName = string.IsNullOrWhiteSpace(_options.DocumentName)
                ? "v1"
                : _options.DocumentName;
        }

        /// <summary>
        /// Gets the name of the served OpenAPI document.
        /// </summary>
        public string DocumentName { get; }

        /// <summary>
        /// Gets the OpenAPI document for the current request, deriving the
        /// server URL from the request scheme and host when not explicitly set
        /// in the options.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>The generated (and cached) OpenAPI document.</returns>
        public OpenApiDocument GetDocument(HttpContext context)
        {
            var scheme = context.Request.Scheme;
            var host = context.Request.Host.ToUriComponent();
            var pathBase = context.Request.PathBase.Value ?? string.Empty;
            var serverUrl = $"{scheme}://{host}{pathBase}/";

            // Fast path: return the cached document without locking when it
            // already matches the requested scheme+host. Reference reads of
            // the cached fields are atomic on .NET.
            if (_cached != null && string.Equals(_cachedForSchemeHost, serverUrl, StringComparison.Ordinal))
                return _cached;

            lock (_sync)
            {
                // Re-check inside the lock — another thread may have built it.
                if (_cached != null && string.Equals(_cachedForSchemeHost, serverUrl, StringComparison.Ordinal))
                    return _cached;

                var opts = _options;
                // When the caller left ServerUrl unset we let the request-derived
                // one win (common in local dev / dynamic hosts).
                if (string.IsNullOrWhiteSpace(opts.ServerUrl))
                {
                    // Options is a shared singleton — we want request-derived
                    // servers on a per-build basis without mutating shared state.
                    var clone = opts.Clone();
                    clone.ServerUrl = serverUrl;
                    opts = clone;
                }

                var builder = new BdOpenApiDocumentBuilder(opts);
                _cached = builder.Build(default);
                _cachedForSchemeHost = serverUrl;
                return _cached;
            }
        }

        /// <summary>Writes the current document to <paramref name="stream"/> as JSON.</summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="stream">The stream to write the JSON document to.</param>
        public void WriteAsJson(HttpContext context, Stream stream)
        {
            var doc = GetDocument(context);
            using var text = new StreamWriter(stream, leaveOpen: true);
            var writer = new OpenApiJsonWriter(text);
            doc.SerializeAsV31(writer);
            text.Flush();
        }
    }
}
