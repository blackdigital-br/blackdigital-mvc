namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Controls the caching scope of a <see cref="CacheAttribute"/>. Maps to the
    /// <c>Cache-Control</c> response header's <c>public</c>/<c>private</c>
    /// directives.
    /// </summary>
    public enum CacheLocation
    {
        /// <summary>
        /// The response may be cached by any cache (shared proxies and the
        /// client). Emits <c>Cache-Control: public</c>.
        /// </summary>
        Any,

        /// <summary>
        /// The response may be cached only by the client (no shared caches).
        /// Emits <c>Cache-Control: private</c>.
        /// </summary>
        Client
    }

    /// <summary>
    /// Marks a REST action as cacheable. When applied to an action method, the
    /// middleware adds a <c>Cache-Control</c> response header with the
    /// configured <see cref="Duration"/> and <see cref="Location"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CacheAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheAttribute"/> class
        /// with the specified cache duration.
        /// </summary>
        /// <param name="duration">The cache duration in seconds.</param>
        public CacheAttribute(int duration)
        {
            Duration = duration;
        }

        /// <summary>
        /// Gets the cache duration in seconds.
        /// </summary>
        public int Duration { get; }

        /// <summary>
        /// Gets or sets the caching scope. Defaults to <see cref="CacheLocation.Any"/>.
        /// </summary>
        public CacheLocation Location { get; set; } = CacheLocation.Any;

        /// <summary>
        /// Gets or sets a value indicating whether the response may be stored by
        /// caches even when it would normally not be stored (e.g. behind
        /// authorization). Emits the <c>no-store</c>-removing <c>private</c>
        /// behavior. Defaults to <c>false</c>.
        /// </summary>
        public bool NoStore { get; set; }
    }
}
