
namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Fluent builder for registering transform rules keyed by
    /// <see cref="TransformKey"/>. Rules can be registered as instances or as
    /// types that the <see cref="TransformManager"/> instantiates through DI.
    /// </summary>
    public class TransformBuilder
    {
        private Dictionary<TransformKey, ITransformRule> _rulesInstances = new();

        private Dictionary<TransformKey, Type> _rulesType = new();


        /// <summary>
        /// Registers a pre-created transform rule instance for the given key.
        /// </summary>
        /// <param name="key">The key that identifies the transform.</param>
        /// <param name="rule">The rule instance to register.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule(TransformKey key, ITransformRule rule)
        {
            _rulesInstances.Add(key, rule);
            return this;
        }

        /// <summary>
        /// Registers a pre-created transform rule instance for the given key,
        /// version and direction.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="rule">The rule instance to register.</param>
        /// <param name="direction">The direction the rule applies to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule(string key, string version, ITransformRule rule, TransformDirection direction = TransformDirection.Input)
            => AddRule(new TransformKey(key, version, direction), rule);

        /// <summary>
        /// Registers a pre-created transform rule instance for input transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="rule">The rule instance to register.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Input), rule);

        /// <summary>
        /// Registers a pre-created transform rule instance for output transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="rule">The rule instance to register.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddOutputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Output), rule);

        /// <summary>
        /// Registers a pre-created transform rule instance for both input and
        /// output transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="rule">The rule instance to register.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputAndOutputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Both), rule);

        /// <summary>
        /// Registers a single pre-created transform rule instance for multiple keys.
        /// </summary>
        /// <param name="rule">The rule instance to register.</param>
        /// <param name="keys">The keys that map to the rule. At least one key is required.</param>
        /// <returns>The current builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no keys are provided.</exception>
        public TransformBuilder AddRule(ITransformRule rule, params TransformKey[] keys)
        {
            if (keys.Length == 0)
                throw new ArgumentException("At least one key must be provided.");

            foreach (var key in keys)
                AddRule(key, rule);

            return this;
        }


        /// <summary>
        /// Registers a transform rule type for the given key. The type is
        /// instantiated through DI by the <see cref="TransformManager"/>.
        /// </summary>
        /// <param name="key">The key that identifies the transform.</param>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ruleType"/> does not implement <see cref="ITransformRule"/>.</exception>
        public TransformBuilder AddRule(TransformKey key, Type ruleType)
        {
            if (!typeof(ITransformRule).IsAssignableFrom(ruleType))
                throw new ArgumentException($"Type '{ruleType.FullName}' does not implement ITransformRule.");

            _rulesType.Add(key, ruleType);
            return this;
        }

        /// <summary>
        /// Registers a transform rule type for the given key, version and direction.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <param name="direction">The direction the rule applies to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule(string key, string version, Type ruleType, TransformDirection direction = TransformDirection.Input)
            => AddRule(new TransformKey(key, version, direction), ruleType);

        /// <summary>
        /// Registers a transform rule type for input transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Input), ruleType);

        /// <summary>
        /// Registers a transform rule type for output transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddOutputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Output), ruleType);

        /// <summary>
        /// Registers a transform rule type for both input and output transforms.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputAndOutputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Both), ruleType);

        /// <summary>
        /// Registers a single transform rule type for multiple keys.
        /// </summary>
        /// <param name="ruleType">The rule type to register. Must implement <see cref="ITransformRule"/>.</param>
        /// <param name="keys">The keys that map to the rule. At least one key is required.</param>
        /// <returns>The current builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when no keys are provided.</exception>
        public TransformBuilder AddRule(Type ruleType, params TransformKey[] keys)
        {
            if (keys.Length == 0)
                throw new ArgumentException("At least one key must be provided.");
            foreach (var key in keys)
                AddRule(key, ruleType);
            return this;
        }


        /// <summary>
        /// Registers a transform rule of type <typeparamref name="TRule"/> for the given key.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="key">The key that identifies the transform.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule<TRule>(TransformKey key)
            where TRule : ITransformRule
            => AddRule(key, typeof(TRule));

        /// <summary>
        /// Registers a transform rule of type <typeparamref name="TRule"/> for the
        /// given key, version and direction.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="direction">The direction the rule applies to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule<TRule>(string key, string version, TransformDirection direction = TransformDirection.Input)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, direction), typeof(TRule));

        /// <summary>
        /// Registers a transform rule of type <typeparamref name="TRule"/> for input transforms.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Input), typeof(TRule));

        /// <summary>
        /// Registers a transform rule of type <typeparamref name="TRule"/> for output transforms.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddOutputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Output), typeof(TRule));

        /// <summary>
        /// Registers a transform rule of type <typeparamref name="TRule"/> for both
        /// input and output transforms.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddInputAndOutputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Both), typeof(TRule));

        /// <summary>
        /// Registers a single transform rule of type <typeparamref name="TRule"/>
        /// for multiple keys.
        /// </summary>
        /// <typeparam name="TRule">The rule type to register. Must implement <see cref="ITransformRule"/>.</typeparam>
        /// <param name="keys">The keys that map to the rule. At least one key is required.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public TransformBuilder AddRule<TRule>(params TransformKey[] keys)
            where TRule : ITransformRule
            => AddRule(typeof(TRule), keys);

        internal Dictionary<TransformKey, ITransformRule> GetRulesInstances() => _rulesInstances;

        internal Dictionary<TransformKey, Type> GetRulesType() => _rulesType;
    }
}
