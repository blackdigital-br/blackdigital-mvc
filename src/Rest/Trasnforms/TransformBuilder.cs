
namespace BlackDigital.AspNet.Rest.Trasnforms
{
    public class TransformBuilder
    {
        private Dictionary<TransformKey, ITransformRule> _rulesInstances = new();

        private Dictionary<TransformKey, Type> _rulesType = new();


        public TransformBuilder AddRule(TransformKey key, ITransformRule rule)
        {
            _rulesInstances.Add(key, rule);
            return this;
        }

        public TransformBuilder AddRule(string key, string version, ITransformRule rule, TransformDirection direction = TransformDirection.Input)
            => AddRule(new TransformKey(key, version, direction), rule);

        public TransformBuilder AddInputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Input), rule);

        public TransformBuilder AddOutputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Output), rule);

        public TransformBuilder AddInputAndOutputRule(string key, string version, ITransformRule rule)
            => AddRule(new TransformKey(key, version, TransformDirection.Both), rule);

        public TransformBuilder AddRule(ITransformRule rule, params TransformKey[] keys)
        {
            if (keys.Length == 0)
                throw new ArgumentException("At least one key must be provided.");

            foreach (var key in keys)
                AddRule(key, rule);

            return this;
        }


        public TransformBuilder AddRule(TransformKey key, Type ruleType)
        {
            if (!typeof(ITransformRule).IsAssignableFrom(ruleType))
                throw new ArgumentException($"Type '{ruleType.FullName}' does not implement ITransformRule.");

            _rulesType.Add(key, ruleType);
            return this;
        }

        public TransformBuilder AddRule(string key, string version, Type ruleType, TransformDirection direction = TransformDirection.Input)
            => AddRule(new TransformKey(key, version, direction), ruleType);

        public TransformBuilder AddInputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Input), ruleType);

        public TransformBuilder AddOutputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Output), ruleType);

        public TransformBuilder AddInputAndOutputRule(string key, string version, Type ruleType)
            => AddRule(new TransformKey(key, version, TransformDirection.Both), ruleType);

        public TransformBuilder AddRule(Type ruleType, params TransformKey[] keys)
        {
            if (keys.Length == 0)
                throw new ArgumentException("At least one key must be provided.");
            foreach (var key in keys)
                AddRule(key, ruleType);
            return this;
        }


        public TransformBuilder AddRule<TRule>(TransformKey key)
            where TRule : ITransformRule
            => AddRule(key, typeof(TRule));

        public TransformBuilder AddRule<TRule>(string key, string version, TransformDirection direction = TransformDirection.Input)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, direction), typeof(TRule));

        public TransformBuilder AddInputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Input), typeof(TRule));

        public TransformBuilder AddOutputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Output), typeof(TRule));

        public TransformBuilder AddInputAndOutputRule<TRule>(string key, string version)
            where TRule : ITransformRule
            => AddRule(new TransformKey(key, version, TransformDirection.Both), typeof(TRule));

        public TransformBuilder AddRule<TRule>(params TransformKey[] keys)
            where TRule : ITransformRule
            => AddRule(typeof(TRule), keys);

        internal Dictionary<TransformKey, ITransformRule> GetRulesInstances() => _rulesInstances;

        internal Dictionary<TransformKey, Type> GetRulesType() => _rulesType;
    }
}
