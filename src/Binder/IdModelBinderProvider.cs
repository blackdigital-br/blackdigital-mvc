
using BlackDigital.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlackDigital.Mvc.Binder
{   
    public class IdModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            if (context.Metadata.ModelType == typeof(Id))
                return new IdBinder();
            
            return null;
        }
    }
}
