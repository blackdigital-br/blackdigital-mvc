using BlackDigital.Mvc.Binder;
using BlackDigital.Mvc.Constraint;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public static class RestHelper
    {
        private static MvcRestBuilder? MvcRestBuilder;

        public static IServiceCollection AddRestService(this IServiceCollection services,
                                               Func<MvcRestBuilder, MvcRestBuilder> restService)
        {
            MvcRestBuilder ??= new(services);
            MvcRestBuilder = restService(MvcRestBuilder);

            return services;
        }

        public static IServiceCollection AddRestControllers(this IServiceCollection services)
        {
            if (MvcRestBuilder == null)
                throw new Exception("You must call AddRestService before UseRestService");

            var controllers = MvcRestBuilder.Build();

            if (controllers.Any())
            {
                services.AddControllers(options => {
                    options.AddDefaultOptions();
                })
                .AddApplicationPart(controllers.First().Assembly);
            }

            MvcRestBuilder = null;

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("id", typeof(IdConstraint));
            });

            return services;
        }

        public static MvcOptions AddDefaultOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new IdModelBinderProvider());

            return options;
        }
    }
}
