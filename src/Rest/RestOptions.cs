namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Configurable options for the <see cref="RestMiddleware"/>. Controls
    /// optional cross-cutting behavior such as request/response logging.
    /// </summary>
    public class RestOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether each matched REST request
        /// should be logged (HTTP method, path, status code and duration).
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool EnableRequestLogging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request body should be
        /// included in the log entry. Only takes effect when
        /// <see cref="EnableRequestLogging"/> is <c>true</c>. Defaults to
        /// <c>false</c> to avoid logging sensitive payloads.
        /// </summary>
        public bool LogRequestBody { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the response body should be
        /// included in the log entry. Only takes effect when
        /// <see cref="EnableRequestLogging"/> is <c>true</c>. Defaults to
        /// <c>false</c>.
        /// </summary>
        public bool LogResponseBody { get; set; }
    }
}
