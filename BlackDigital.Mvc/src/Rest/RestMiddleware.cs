using Microsoft.AspNetCore.Http;

namespace BlackDigital.Mvc.Rest
{
    public class RestMiddleware
    {
        public RestMiddleware(RequestDelegate next, RestServiceList restService)
        {
            if (restService is null)
                throw new ArgumentNullException(nameof(restService));

            Next = next;
            RestServices = restService;
        }

        private readonly RequestDelegate Next;
        private readonly RestServiceList RestServices;

        public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
        {
            if (await RestServices.FindAndExecuteService(context, serviceProvider))
                return;

            await Next(context);
        }
    }
}
