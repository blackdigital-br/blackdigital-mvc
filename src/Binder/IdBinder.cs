using BlackDigital.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlackDigital.Mvc.Binder
{
    /// <summary>
    /// Model binder personalizado que converte valores de string em objetos do tipo Id.
    /// Este binder é responsável por interceptar requisições que contêm parâmetros do tipo Id
    /// e realizar a conversão automática de strings para instâncias da classe Id do BlackDigital.Model.
    /// </summary>
    /// <remarks>
    /// O IdBinder implementa a interface IModelBinder e é usado pelo sistema de model binding do ASP.NET Core
    /// para automaticamente converter valores de entrada (como parâmetros de rota, query string, ou form data)
    /// em objetos Id. Se o valor fornecido for nulo, vazio ou apenas espaços em branco, o binding falhará silenciosamente.
    /// Caso contrário, um novo objeto Id será criado com o valor fornecido.
    /// </remarks>
    public class IdBinder : IModelBinder
    {
        /// <summary>
        /// Executa o processo de model binding para converter um valor de string em um objeto Id.
        /// </summary>
        /// <param name="bindingContext">O contexto de model binding que contém informações sobre a requisição e o modelo a ser vinculado.</param>
        /// <returns>Uma Task que representa a operação assíncrona de binding.</returns>
        /// <remarks>
        /// Este método segue o seguinte fluxo:
        /// 1. Obtém o valor do ValueProvider usando o FieldName do contexto
        /// 2. Se o valor for None, retorna sem fazer nada
        /// 3. Define o valor no ModelState para validação
        /// 4. Se o valor for nulo, vazio ou apenas espaços, retorna sem definir o resultado
        /// 5. Caso contrário, cria um novo objeto Id e define o resultado como Success
        /// </remarks>
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
