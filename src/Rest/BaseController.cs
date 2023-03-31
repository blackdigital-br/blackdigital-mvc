using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BlackDigital.Mvc.Rest
{
    public abstract class BaseController<IEntityController> : Controller
    {
        public BaseController(IEntityController entityController)
        {
            if (entityController == null)
                throw new ArgumentNullException(nameof(entityController));

            EntityController = entityController;
        }

        protected readonly IEntityController EntityController;

        protected async Task<ActionResult> ExecuteActionAsync(string name, Dictionary<string, object> arguments)
        {
            if (EntityController == null)
                throw new ArgumentNullException(nameof(EntityController));

            var methodInfo = EntityController.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(x => x.Name == name && x.GetParameters().Length == arguments.Count)
                                             .SingleOrDefault();

            if (methodInfo == null)
                throw new MethodAccessException($"{name}: {arguments.Count}");

            try
            {
                var response = methodInfo.Invoke(EntityController, arguments.Values.ToArray());

                if (methodInfo.ReturnType != typeof(void))
                {
                    if (response is Task task)
                    {
                        await task;

                        if (methodInfo.ReturnType.IsGenericType)
                        {
                            var result = methodInfo.ReturnType.GetProperty("Result")?.GetValue(task);
                            return Ok(result);
                        }
                    }

                    return Ok(response);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                if (ex.InnerException is BusinessException businessException)
                    return StatusCode(businessException.Code, businessException.Message);

                throw ex.InnerException ?? ex;
            }
        }
    }
}
