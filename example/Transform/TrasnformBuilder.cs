using BlackDigital.Rest.Transforms;

namespace BlackDigital.Mvc.Example.Transform
{
    public class TrasnformBuilder
    {
        public TrasnformBuilder(TransformManager transformManager, IServiceProvider serviceProvider)
        { 
            _transformManager = transformManager;
            _serviceProvider = serviceProvider;
        }   

        private readonly TransformManager _transformManager;
        private readonly IServiceProvider _serviceProvider;

        public TrasnformBuilder AddRule(TransformKey key, ITransformRule rule)
        {
            _transformManager.AddRule(key, rule);
            return this;
        }

        public TrasnformBuilder AddRule(TransformKey key, Type ruleType)
        {
            if (!typeof(ITransformRule).IsAssignableFrom(ruleType))
                throw new ArgumentException("ruleType must implement ITransformRule", nameof(ruleType));

            var rule = (ITransformRule)ActivatorUtilities.CreateInstance(_serviceProvider, ruleType);

            return AddRule(key, rule);
        }
    }
}
