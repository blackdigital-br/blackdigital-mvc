using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlackDigital.AspNet.Filters
{
    /// <summary>
    /// Action filter that intercepts invalid model states and converts them
    /// into an <see cref="ErrorApiResponse"/> returned with a 400 Bad Request.
    /// </summary>
    public class ModelStateValidationFilter : IActionFilter
    {
        /// <summary>
        /// Called before the action executes. When the model state is invalid,
        /// the request is short-circuited with a 400 Bad Request containing
        /// the validation errors.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var errorResponse = ErrorApiResponse.Create(
                    "Validation failed",
                    errors.Any() ? errors : new List<string> { "Invalid request data" }
                );

                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }

        /// <summary>
        /// Called after the action executes. No action is performed by this filter.
        /// </summary>
        /// <param name="context">The action executed context.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}
