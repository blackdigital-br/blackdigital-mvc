using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public static class RestHelper
    {
        public static IServiceCollection AddRestService(this IServiceCollection services,
                                               Func<MvcRestBuilder, MvcRestBuilder> restService)
        {
            MvcRestBuilder mvcRestBuilder = new(services);
            mvcRestBuilder = restService(mvcRestBuilder);

            RestServiceList restServiceList = new(mvcRestBuilder.Services);
            services.AddSingleton(restServiceList);
            return services;
        }

        public static IApplicationBuilder UseRestService(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<RestMiddleware>();
        }
    }
}
