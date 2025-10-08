using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest.Trasnforms
{
    public static class TransformExtensions
    {
        public static IServiceCollection AddTransform(this IServiceCollection services,
            Func<TransformBuilder, TransformBuilder> configBuilder)
        {
            var builder = new TransformBuilder();
            builder = configBuilder(builder);
            services.AddSingleton(builder);
            services.AddScoped<TransformManager>();

            return services;
        }
    }
}
