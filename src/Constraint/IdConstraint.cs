using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlackDigital.Mvc.Constraint
{
    public class IdConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values.TryGetValue(routeKey, out var value))
            {
                if (value is string id)
                {
                    return true;
                    //return id.Length == 24 && id.All(char.IsDigit);
                }
            }
            return false;
        }
    }
}
