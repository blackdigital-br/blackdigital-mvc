using BlackDigital.Mvc.Binder;
using BlackDigital.Mvc.Constraint;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.Mvc.Rest
{
    public static class RestHelper
    {

        /// <summary>
        /// Configura as opções padrão do MVC incluindo model binders e constraints
        /// </summary>
        public static IServiceCollection AddRestMvcOptions(this IServiceCollection services)
        {
            services.AddControllers(options => {
                options.AddDefaultOptions();
            });

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("id", typeof(IdConstraint));
            });

            return services;
        }

        /// <summary>
        /// Adiciona as opções padrão do MVC incluindo model binders personalizados
        /// </summary>
        /// <param name="options">As opções do MVC a serem configuradas</param>
        /// <returns>As opções do MVC configuradas para permitir encadeamento de métodos</returns>
        public static MvcOptions AddDefaultOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new IdModelBinderProvider());

            return options;
        }
    }
}
