using BlackDigital.Rest;
using System.Reflection;

namespace BlackDigital.Mvc.Rest
{
    internal class RestServiceItem
    {
        internal RestServiceItem(Type service)
        {
            Type = service;
            Attribute = service.GetCustomAttribute<ServiceAttribute>()
                            ?? throw new NullReferenceException("ServiceAttribute");

            Actions = service.GetMethods()
                             .Select(methodInfo => new ActionServiceItem(this, methodInfo))
                             .ToArray();
        }

        internal readonly Type Type;
        internal readonly ServiceAttribute Attribute;
        internal readonly ActionServiceItem[] Actions;
    }
}
