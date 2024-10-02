
using BlackDigital.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace BlackDigital.Mvc.Rest
{
    public class RestRouterMiddleware
    {
        public RestRouterMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public async Task InvokeAsync(HttpContext context)
        {
            var pathSegments = context.Request.Path.Value?.Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (pathSegments == null || pathSegments.Count == 0)
            {
                await _next(context);
                return;
            }

            foreach (var type in MvcRestBuilder.Services)
            {
                var service = type.GetCustomAttribute<ServiceAttribute>();
                if (service == null)
                    continue;
                
                var method = context.Request.Method;


                foreach (var route in MvcRestBuilder.Routes)
                {
                    if (route.IsMatch(method, pathSegments))
                    {
                        var domainInstance = _serviceProvider.GetService(route.DomainType);

                        route.Method.Invoke(domainInstance, new object[] { context });
                    }
                }
                

                /*service.Match

                var methods = type.GetMethods();
                

                foreach (var method in methods)
                {
                    var action = method.GetCustomAttribute<ActionAttribute>();
                    if (action == null)
                        continue;
                }*/
            }
        }
    }
}
