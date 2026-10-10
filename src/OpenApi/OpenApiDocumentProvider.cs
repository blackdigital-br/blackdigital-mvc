using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    public sealed class OpenApiDocumentProvider : IOpenApiDocumentProvider
    {
        private readonly object _sync = new();
        private readonly BdOpenApiOptions _options;
        private OpenApiDocument? _cached;
        private string? _cachedForSchemeHost;

        public OpenApiDocumentProvider(IOptions<BdOpenApiOptions> options)
        {
            _options = options.Value ?? new BdOpenApiOptions();
            DocumentName = string.IsNullOrWhiteSpace(_options.DocumentName)
                ? "v1"
                : _options.DocumentName;
        }

        public string DocumentName { get; }

        public OpenApiDocument GetDocument(HttpContext context)
        {
            var scheme = context.Request.Scheme;
            var host = context.Request.Host.ToUriComponent();
            var pathBase = context.Request.PathBase.Value ?? string.Empty;
            var serverUrl = $"{scheme}://{host}{pathBase}/";

            lock (_sync)
            {
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
