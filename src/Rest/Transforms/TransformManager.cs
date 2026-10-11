using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Resolves and applies transform rules registered via
    /// <see cref="TransformBuilder"/>. Rules are matched by key, direction and
    /// version, with version-based ordering determining the execution sequence.
    /// </summary>
    public class TransformManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformManager"/> class.
        /// Loads all rules from the supplied builder, instantiating type-based
        /// rules through the provided service provider.
        /// </summary>
        /// <param name="builder">The builder containing the registered rules.</param>
        /// <param name="serviceProvider">The service provider used to instantiate type-based rules.</param>
        public TransformManager(TransformBuilder builder, IServiceProvider serviceProvider)
        {
            _builder = builder;
            _serviceProvider = serviceProvider;
            LoadRulesFromBuilder();
        }

        private readonly TransformBuilder _builder;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<TransformKey, ITransformRule> _rules = new();

        
        private void LoadRulesFromBuilder()
        {
            foreach (var kvp in _builder.GetRulesInstances())
            {
                _rules[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in _builder.GetRulesType())
            {
                if (!_rules.ContainsKey(kvp.Key))
                {
                    var ruleType = kvp.Value;
                    var ruleInstance = (ITransformRule)ActivatorUtilities.CreateInstance(_serviceProvider, ruleType);

                    if (ruleInstance != null)
                    {
                        _rules[kvp.Key] = ruleInstance;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether at least one rule is registered for the given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns><c>true</c> if a rule exists for the key; otherwise, <c>false</c>.</returns>
        public bool HasRule(TransformKey key)
            => _rules.ContainsKey(key);

        /// <summary>
        /// Determines whether at least one rule is registered for the given key,
        /// version and direction.
        /// </summary>
        /// <param name="key">The logical key to look up.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="direction">The direction the rule applies to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns><c>true</c> if a rule exists; otherwise, <c>false</c>.</returns>
        public bool HasRule(string key, string version, TransformDirection direction = TransformDirection.Input)
            => HasRule(new TransformKey(key, version, direction));

        /// <summary>
        /// Returns the rules that should be applied for the given key, ordered by
        /// version. Only rules registered with a version strictly greater than the
        /// requested version are returned.
        /// </summary>
        /// <param name="key">The key whose rules are requested.</param>
        /// <returns>
        /// A list of matching rules ordered by version, or <c>null</c> when no
        /// rules match the key.
        /// </returns>
        public IList<ITransformRule>? GetRequiredRules(TransformKey key)
        {
            // A rule registered with TransformDirection.Both must match both
            // Input and Output queries, so use a bitwise intersection rather
            // than strict equality against the queried direction.
            var matchingRulesQuery = _rules
                .Where(kvp => kvp.Key.Key == key.Key &&
                             (kvp.Key.Direction & key.Direction) == key.Direction &&
                             string.Compare(kvp.Key.Version, key.Version, StringComparison.Ordinal) > 0);

            if (key.Direction == TransformDirection.Input)
                matchingRulesQuery = matchingRulesQuery.OrderBy(kvp => kvp.Key.Version);
            else
                matchingRulesQuery = matchingRulesQuery.OrderByDescending(kvp => kvp.Key.Version);

            var matchingRules = matchingRulesQuery
                .Select(kvp => kvp.Value)
                .ToList();
            
            if (matchingRules.Count == 0)
                return null;

            return matchingRules;
        }

        /// <summary>
        /// Returns the input type of the first matching rule for the given key.
        /// </summary>
        /// <param name="key">The key whose first rule's input type is requested.</param>
        /// <returns>The input type of the first rule, or <c>null</c> when no rules match.</returns>
        public Type? GetFirstInputType(TransformKey key)
        {
            var rules = GetRequiredRules(key);

            if (rules == null || rules.Count == 0)
                return null;

            return rules[0].InputType;
        }

        /// <summary>
        /// Returns the rules that should be applied for the given key, version and direction.
        /// </summary>
        /// <param name="key">The logical key whose rules are requested.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="direction">The direction the rules apply to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>
        /// A list of matching rules ordered by version, or <c>null</c> when no
        /// rules match.
        /// </returns>
        public IList<ITransformRule>? GetRequiredRules(string key, string version, TransformDirection direction = TransformDirection.Input)
            => GetRequiredRules(new TransformKey(key, version, direction));

        /// <summary>
        /// Synchronously applies all matching rules to the given value.
        /// </summary>
        /// <param name="key">The key identifying which rules to apply.</param>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value, or the original value when no rules match.</returns>
        public object? Transform(TransformKey key, object? value)
        {
            var rules = GetRequiredRules(key);

            if (rules == null)
            {
                return value;
            }

            var result = value;

            foreach (var rule in rules)
            {
                result = rule.Transform(result);
            }

            return result;
        }

        /// <summary>
        /// Synchronously applies all matching rules to the given value.
        /// </summary>
        /// <param name="key">The logical key identifying which rules to apply.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="value">The value to transform.</param>
        /// <param name="direction">The direction the rules apply to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>The transformed value, or the original value when no rules match.</returns>
        public object? Transform(string key, string version, object? value, TransformDirection direction = TransformDirection.Input)
            => Transform(new TransformKey(key, version, direction), value);

        /// <summary>
        /// Asynchronously applies all matching rules to the given value.
        /// </summary>
        /// <param name="key">The key identifying which rules to apply.</param>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value, or the original value when no rules match.</returns>
        public async Task<object?> TransformAsync(TransformKey key, object? value)
        {
            var rules = GetRequiredRules(key);

            if (rules == null)
            {
                return value;
            }

            var result = value;

            foreach (var rule in rules)
            {
                result = await rule.TransformAsync(result);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously applies all matching rules to the given value.
        /// </summary>
        /// <param name="key">The logical key identifying which rules to apply.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="value">The value to transform.</param>
        /// <param name="direction">The direction the rules apply to. Defaults to <see cref="TransformDirection.Input"/>.</param>
        /// <returns>A task that yields the transformed value, or the original value when no rules match.</returns>
        public async Task<object?> TransformAsync(string key, string version, object? value, TransformDirection direction = TransformDirection.Input)
            => await TransformAsync(new TransformKey(key, version, direction), value);
    }
}
