using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public static class RestHelper
    {

        private static MvcRestBuilder MvcRestBuilder;

        public static IServiceCollection AddRestService(this IServiceCollection services,
                                               Func<MvcRestBuilder, MvcRestBuilder> restService)
        {
            MvcRestBuilder ??= new(services);
            MvcRestBuilder = restService(MvcRestBuilder);

            return services;
        }

        public static IServiceCollection UseRestService(this IServiceCollection services)
        {
            if (MvcRestBuilder == null)
            {
                throw new Exception("You must call AddRestService before UseRestService");
            }

            var controllers = MvcRestBuilder.Build();

            if (controllers.Any())
            {
                services.AddControllers()
                        .AddApplicationPart(controllers.First().Assembly);
            }

            return services;
        }
    }
}
