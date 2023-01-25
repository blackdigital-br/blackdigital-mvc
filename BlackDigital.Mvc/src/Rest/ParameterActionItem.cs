using BlackDigital.Rest;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlackDigital.Mvc.Rest
{
    internal class ParameterActionItem
    {
        internal ParameterActionItem(ParameterInfo parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter));

            Name = parameter.Name ?? string.Empty;
            Type = parameter.ParameterType;
            Route = parameter.GetCustomAttribute<FromRouteAttribute>();
            Header = parameter.GetCustomAttribute<FromHeaderAttribute>();
            Body = parameter.GetCustomAttribute<FromBodyAttribute>();
            Query = parameter.GetCustomAttribute<FromQueryAttribute>();
        }

        internal readonly string Name;
        internal readonly Type Type;
        internal readonly FromRouteAttribute? Route;
        internal readonly FromHeaderAttribute? Header;
        internal readonly FromBodyAttribute? Body;
        internal readonly FromQueryAttribute? Query;


    }
}
