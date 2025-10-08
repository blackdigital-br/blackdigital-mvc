using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest.Trasnforms
{
    public class TransformManager
    {
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

        public bool HasRule(TransformKey key)
            => _rules.ContainsKey(key);

        public bool HasRule(string key, string version, TransformDirection direction = TransformDirection.Input)
            => HasRule(new TransformKey(key, version, direction));

        public IList<ITransformRule>? GetRequiredRules(TransformKey key)
        {
            var matchingRulesQuery = _rules
                .Where(kvp => kvp.Key.Key == key.Key &&
                             kvp.Key.Direction == key.Direction &&
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

        public Type? GetFirstInputType(TransformKey key)
        {
            var rules = GetRequiredRules(key);

            if (rules == null || rules.Count == 0)
                return null;

            return rules[0].InputType;
        }

        public IList<ITransformRule>? GetRequiredRules(string key, string version, TransformDirection direction = TransformDirection.Input)
            => GetRequiredRules(new TransformKey(key, version, direction));

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

        public object? Transform(string key, string version, object? value, TransformDirection direction = TransformDirection.Input)
            => Transform(new TransformKey(key, version, direction), value);

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

        public async Task<object?> TransformAsync(string key, string version, object? value, TransformDirection direction = TransformDirection.Input)
            => await TransformAsync(new TransformKey(key, version, direction), value);
    }
}
