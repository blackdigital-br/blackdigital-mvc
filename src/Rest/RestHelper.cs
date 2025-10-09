using BlackDigital.Converters;
using BlackDigital.AspNet.Binder;
using BlackDigital.AspNet.Constraint;
using BlackDigital.AspNet.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Rest
{
    public static class RestHelper
    {

        /// <summary>
        /// Configura as opções padrão do MVC incluindo model binders e constraints
        /// </summary>
        public static IServiceCollection AddRestMvcOptions(this IServiceCollection services)
        {
            services.AddControllers(options => {
                options.AddDefaultOptions()
                       .Filters.Add<ModelStateValidationFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

                options.JsonSerializerOptions.Converters.Add(new IdConverter());
                options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
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
