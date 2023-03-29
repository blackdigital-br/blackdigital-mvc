using Microsoft.AspNetCore.Http;
using System.IO;

namespace BlackDigital.Mvc.Rest
{
    public class RestServiceList
    {
        public RestServiceList(IEnumerable<Type> services)
        {
            Services = services.Select(service => new RestServiceItem(service))
                               .ToArray();
        }

        private readonly RestServiceItem[] Services;

        internal async Task<bool> FindAndExecuteService(HttpContext context, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(context.Request.Path))
                return false;

            var scopedPath = context.Request.Path.ToString().ToLower();

            if (scopedPath.StartsWith('/'))
                scopedPath = scopedPath[1..];

            if (scopedPath.EndsWith('/'))
                scopedPath = scopedPath[..^1];

            var service = FindService(context, ref scopedPath);

            if (service != null)
            {
                var action = service.Actions.SingleOrDefault(action => action.IsMatch(context, scopedPath));
                
                if (action != null)
                {
                    await action.ExecuteActionAsync(context, serviceProvider, scopedPath);
                    return true;
                }
            }

            return false;
        }

        private RestServiceItem? FindService(HttpContext context, ref string scopedPath)
        {
            var path = scopedPath;

            var service = Services.SingleOrDefault(service => path.StartsWith(service.Attribute.BaseRoute?.ToLower()
                                                                                ?? string.Empty));

            if (service != null)
            {
                int length = service.Attribute.BaseRoute.Length;

                if (length < scopedPath.Length)
                    length++;

                scopedPath = scopedPath[length..];
            }

            return service;
        }
    }
}
