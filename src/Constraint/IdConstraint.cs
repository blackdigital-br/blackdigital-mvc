using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlackDigital.AspNet.Constraint
{
    /// <summary>
    /// Custom route constraint that validates if a route value is a valid string representation of an ID.
    /// This constraint ensures that the route parameter exists and is of type string.
    /// </summary>
    public class IdConstraint : IRouteConstraint
    {
        /// <summary>
        /// Determines whether the route value associated with the specified route key is a valid string ID.
        /// </summary>
        /// <param name="httpContext">The HTTP context for the current request.</param>
        /// <param name="route">The router that this constraint belongs to.</param>
        /// <param name="routeKey">The name of the route parameter to validate.</param>
        /// <param name="values">A dictionary that contains the route values for the URL.</param>
        /// <param name="routeDirection">An object that indicates whether the constraint check is being performed when an incoming request is being handled or when a URL is being generated.</param>
        /// <returns>true if the route value exists and is a string; otherwise, false.</returns>
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
