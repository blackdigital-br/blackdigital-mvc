using BlackDigital.Rest;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Cached, pre-computed binding metadata for a single method parameter. Built
    /// once per <see cref="ParameterInfo"/> and reused across requests to avoid
    /// repeated reflection (attribute lookups, type checks) on the hot path.
    /// </summary>
    public sealed class ParameterMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterMetadata"/>
        /// class by inspecting the provided parameter's binding attributes and
        /// type information.
        /// </summary>
        /// <param name="parameter">The parameter to inspect.</param>
        public ParameterMetadata(ParameterInfo parameter)
        {
            ParameterType = parameter.ParameterType;
            Name = parameter.Name;
            HasDefaultValue = parameter.HasDefaultValue;
            DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null;

            RouteAttribute = parameter.GetCustomAttribute<PathAttribute>();
            BodyAttribute = parameter.GetCustomAttribute<BodyAttribute>();
            HeaderAttribute = parameter.GetCustomAttribute<HeaderAttribute>();
            QueryAttribute = parameter.GetCustomAttribute<QueryAttribute>();

            RouteKey = RouteAttribute?.Name ?? Name;
            HeaderKey = HeaderAttribute?.Name ?? Name;
            QueryKey = QueryAttribute?.Name ?? Name;

            IsCancellationToken = ParameterType == typeof(CancellationToken);
            IsBody = BodyAttribute != null;
            IsFormFile = ParameterType == typeof(IFormFile);
            IsFormFileCollection = ParameterType == typeof(IFormFileCollection);
            IsSimpleType = RestParameterTypeCache.IsSimpleType(ParameterType);
        }

        /// <summary>Gets the parameter's declared type.</summary>
        public Type ParameterType { get; }

        /// <summary>Gets the parameter name.</summary>
        public string? Name { get; }

        /// <summary>Gets a value indicating whether the parameter has a default value.</summary>
        public bool HasDefaultValue { get; }

        /// <summary>Gets the parameter's default value, or null when it has none.</summary>
        public object? DefaultValue { get; }

        /// <summary>Gets the <c>[Path]</c> attribute, or null when absent.</summary>
        public PathAttribute? RouteAttribute { get; }

        /// <summary>Gets the <c>[Body]</c> attribute, or null when absent.</summary>
        public BodyAttribute? BodyAttribute { get; }

        /// <summary>Gets the <c>[Header]</c> attribute, or null when absent.</summary>
        public HeaderAttribute? HeaderAttribute { get; }

        /// <summary>Gets the <c>[Query]</c> attribute, or null when absent.</summary>
        public QueryAttribute? QueryAttribute { get; }

        /// <summary>Gets the route parameter key (attribute name or parameter name).</summary>
        public string? RouteKey { get; }

        /// <summary>Gets the header key (attribute name or parameter name).</summary>
        public string? HeaderKey { get; }

        /// <summary>Gets the query string key (attribute name or parameter name).</summary>
        public string? QueryKey { get; }

        /// <summary>Gets a value indicating whether the parameter is a <see cref="CancellationToken"/>.</summary>
        public bool IsCancellationToken { get; }

        /// <summary>Gets a value indicating whether the parameter is a body parameter.</summary>
        public bool IsBody { get; }

        /// <summary>Gets a value indicating whether the parameter is <see cref="IFormFile"/>.</summary>
        public bool IsFormFile { get; }

        /// <summary>Gets a value indicating whether the parameter is <see cref="IFormFileCollection"/>.</summary>
        public bool IsFormFileCollection { get; }

        /// <summary>Gets a value indicating whether the parameter is a simple (string-convertible) type.</summary>
        public bool IsSimpleType { get; }
    }

    /// <summary>
    /// Provides a cached, pre-computed list of <see cref="ParameterMetadata"/>
    /// for each <see cref="MethodInfo"/>, avoiding repeated reflection on every
    /// request.
    /// </summary>
    public static class ParameterMetadataCache
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<MethodInfo, ParameterMetadata[]> _cache = new();

        /// <summary>
        /// Returns the cached <see cref="ParameterMetadata"/> array for the given
        /// method, building it on first access.
        /// </summary>
        /// <param name="method">The method whose parameters are inspected.</param>
        /// <returns>An array of <see cref="ParameterMetadata"/> matching the method's parameter count.</returns>
        public static ParameterMetadata[] Get(MethodInfo method)
            => _cache.GetOrAdd(method, static m =>
            {
                var parameters = m.GetParameters();
                var metadata = new ParameterMetadata[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                    metadata[i] = new ParameterMetadata(parameters[i]);
                return metadata;
            });
    }

    /// <summary>
    /// Caches the "is simple type" determination for parameter types, avoiding
    /// repeated reflection-based type checks on every request.
    /// </summary>
    internal static class RestParameterTypeCache
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _simpleTypeCache = new();

        /// <summary>
        /// Determines whether a type is a "simple" value type that can be
        /// converted from a string (primitives, string, decimal, date/time
        /// types, Guid, enums, and the framework Id type).
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> when the type is simple; otherwise, <c>false</c>.</returns>
        public static bool IsSimpleType(Type type)
            => _simpleTypeCache.GetOrAdd(type, static t =>
            {
                var underlying = Nullable.GetUnderlyingType(t) ?? t;
                if (underlying.IsPrimitive) return true;
                if (underlying == typeof(string) || underlying == typeof(decimal)
                    || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
                    || underlying == typeof(Guid)) return true;
                if (underlying.IsEnum) return true;
                if (underlying.FullName == "BlackDigital.Model.Id") return true;
                return false;
            });
    }
}
