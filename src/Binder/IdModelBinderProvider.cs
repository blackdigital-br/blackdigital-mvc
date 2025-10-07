
using BlackDigital.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlackDigital.Mvc.Binder
{
    /// <summary>
    /// Provedor de model binder personalizado que fornece o <see cref="IdBinder"/> 
    /// para propriedades do tipo <see cref="Id"/>.
    /// Esta classe é responsável por determinar quando usar o IdBinder baseado no tipo do modelo.
    /// </summary>
    public class IdModelBinderProvider : IModelBinderProvider
    {
        /// <summary>
        /// Retorna um <see cref="IdBinder"/> se o tipo do modelo for <see cref="Id"/>, 
        /// caso contrário retorna null.
        /// </summary>
        /// <param name="context">O contexto do provedor de model binder contendo metadados do modelo.</param>
        /// <returns>
        /// Uma instância de <see cref="IdBinder"/> se o tipo do modelo for <see cref="Id"/>;
        /// caso contrário, null.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Lançado quando <paramref name="context"/> é null.
        /// </exception>
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
