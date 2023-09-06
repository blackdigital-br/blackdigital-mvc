using BlackDigital.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlackDigital.Mvc.Binder
{
    public class IdBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrWhiteSpace(value))
                return Task.CompletedTask;

            Id id = new (value);
            bindingContext.Result = ModelBindingResult.Success(id);

            return Task.CompletedTask;
        }
    }
}
