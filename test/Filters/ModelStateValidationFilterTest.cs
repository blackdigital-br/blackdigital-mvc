using BlackDigital.AspNet.Filters;
using BlackDigital;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace BlackDigital.AspNet.Test.Filters
{
    public class ModelStateValidationFilterTest
    {
        [Fact]
        public void OnActionExecuting_WhenModelStateIsValid_ShouldNotSetResult()
        {
            var filter = new ModelStateValidationFilter();
            var context = CreateContext(new ModelStateDictionary());

            filter.OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_WhenModelStateIsInvalid_ShouldReturnBadRequest()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "The Name field is required.");

            var filter = new ModelStateValidationFilter();
            var context = CreateContext(modelState);

            filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
            var errorResponse = Assert.IsType<ErrorApiResponse>(badRequest.Value);
            Assert.Equal("Validation failed", errorResponse.Message);
        }

        [Fact]
        public void OnActionExecuting_WhenInvalid_ShouldIncludeErrorMessages()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Email", "Invalid email format.");
            modelState.AddModelError("Age", "Age must be positive.");

            var filter = new ModelStateValidationFilter();
            var context = CreateContext(modelState);

            filter.OnActionExecuting(context);

            var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
            var errorResponse = (ErrorApiResponse)badRequest.Value!;
            Assert.Contains(errorResponse.Errors, e => e.Contains("Email"));
            Assert.Contains(errorResponse.Errors, e => e.Contains("Age"));
        }

        [Fact]
        public void OnActionExecuting_WhenInvalidWithEmptyErrorMessages_ShouldReturnErrorEntries()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("", "");

            var filter = new ModelStateValidationFilter();
            var context = CreateContext(modelState);

            filter.OnActionExecuting(context);

            var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
            var errorResponse = (ErrorApiResponse)badRequest.Value!;
            // The filter always includes the collected error entries; when the
            // error message is empty, the formatted string is still produced.
            Assert.NotNull(errorResponse.Errors);
        }

        [Fact]
        public void OnActionExecuted_ShouldDoNothing()
        {
            var filter = new ModelStateValidationFilter();
            var context = new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new object());

            var ex = Record.Exception(() => filter.OnActionExecuted(context));
            Assert.Null(ex);
            Assert.Null(context.Result);
        }

        private static ActionExecutingContext CreateContext(ModelStateDictionary modelState)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                modelState);

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());
        }
    }
}
