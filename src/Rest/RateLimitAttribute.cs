namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Applies a rate limit to a REST action or an entire service interface.
    /// When the configured number of requests within the time window is
    /// exceeded, the middleware responds with HTTP 429 Too Many Requests.
    /// </summary>
    /// <remarks>
    /// The counter is stored in an <c>IMemoryCache</c> resolved from the request
    /// services, keyed by action key and the client IP address (or the
    /// authenticated user id when available).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class RateLimitAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitAttribute"/>
        /// class.
        /// </summary>
        /// <param name="limit">The maximum number of requests allowed within the window.</param>
        /// <param name="windowSeconds">The length of the rate-limit window in seconds.</param>
        public RateLimitAttribute(int limit, int windowSeconds)
        {
            Limit = limit;
            WindowSeconds = windowSeconds;
        }

        /// <summary>
        /// Gets the maximum number of requests allowed within the window.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Gets the length of the rate-limit window in seconds.
        /// </summary>
        public int WindowSeconds { get; }
    }
}
