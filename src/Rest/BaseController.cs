using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BlackDigital.Mvc.Rest
{
    public abstract class BaseController<IEntityController> : Controller
    {
        public BaseController(IEntityController useCase)
        {
            if (useCase == null)
                throw new ArgumentNullException(nameof(useCase));

            EntityController = useCase;
        }

        protected readonly IEntityController EntityController;

        protected async Task<T?> ExecuteActionAsync<T>(string name, Dictionary<string, object> arguments)
        {
            var methodInfo = EntityController.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(x => x.Name == name && x.GetParameters().Length == arguments.Count)
                                             .SingleOrDefault();

            if (methodInfo == null)
                throw new MethodAccessException($"{name}: {arguments.Count}");

            
            var response = methodInfo.Invoke(EntityController, arguments.Values.ToArray());

            if (response is Task<T> task)
            {
                var result = await task;
                return result;
            }

            return response is T ? (T)response : default;
        }
    }
}
