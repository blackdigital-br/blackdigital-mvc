using BlackDigital.AspNet.Rest.Transforms;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Declaratively registers a transform rule for a REST action. Applied to a
    /// method decorated with <c>ActionAttribute</c>, it binds a
    /// <see cref="ITransformRule"/> to the action's key, version and direction
    /// without requiring registration in <c>Program.cs</c>.
    /// </summary>
    /// <remarks>
    /// The rule type must implement <see cref="ITransformRule"/> and have a
    /// public constructor that can be resolved through dependency injection.
    /// The action key is derived automatically from the service base route and
    /// the action route, matching the key used at runtime by
    /// <c>RestMiddleware</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TransformAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformAttribute"/>
        /// class.
        /// </summary>
        /// <param name="ruleType">
        /// The transform rule type to apply. Must implement
        /// <see cref="ITransformRule"/>.
        /// </param>
        /// <param name="version">
        /// The API version associated with the transform. Rules are applied in
        /// version order for input (ascending) and output (descending).
        /// </param>
        /// <param name="direction">
        /// The direction the rule applies to. Defaults to
        /// <see cref="TransformDirection.Input"/>.
        /// </param>
        public TransformAttribute(Type ruleType, string version, TransformDirection direction = TransformDirection.Input)
        {
            RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));

            if (!typeof(ITransformRule).IsAssignableFrom(ruleType))
                throw new ArgumentException($"Type '{ruleType.FullName}' does not implement ITransformRule.", nameof(ruleType));

            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version cannot be null or whitespace.", nameof(version));

            Version = version;
            Direction = direction;
        }

        /// <summary>
        /// Gets the transform rule type to apply.
        /// </summary>
        public Type RuleType { get; }

        /// <summary>
        /// Gets the API version associated with the transform.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the direction the rule applies to.
        /// </summary>
        public TransformDirection Direction { get; }
    }
}
