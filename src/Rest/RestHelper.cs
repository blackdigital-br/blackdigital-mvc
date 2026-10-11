using BlackDigital.Converters;
using BlackDigital.AspNet.Binder;
using BlackDigital.AspNet.Constraint;
using BlackDigital.AspNet.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Helper methods for configuring MVC options and REST-related services
    /// (custom model binders, route constraints and JSON converters).
    /// </summary>
    public static class RestHelper
    {

        /// <summary>
        /// Configures the default MVC options, registering custom model binders,
        /// route constraints and JSON converters required by the BlackDigital
        /// REST infrastructure.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRestMvcOptions(this IServiceCollection services)
        {
            services.AddControllers(options => {
                options.AddDefaultOptions()
                       .Filters.Add<ModelStateValidationFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new IdConverter());
                options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });


            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("id", typeof(IdConstraint));
            });

            return services;
        }

        /// <summary>
        /// Adds the default MVC options, including the custom
        /// <see cref="IdModelBinderProvider"/> at the top of the binder list.
        /// </summary>
        /// <param name="options">The MVC options to configure.</param>
        /// <returns>The configured MVC options for chaining.</returns>
        public static MvcOptions AddDefaultOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new IdModelBinderProvider());

            return options;
        }
    }
}
