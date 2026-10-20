using BlackDigital.Domain.Security;
using BlackDigital.Rest;

namespace BlackDigital.AspNet.Test.OpenApi
{
    // Test service interfaces used by BdOpenApiDocumentBuilderTest.
    // These are scanned from the test assembly when the builder's
    // assembly prefix filter is configured to include "BlackDigital.AspNet.Test".
    //
    // Access requirements ([Authorize]) are declared on the implementation
    // classes below, never on the interface, mirroring the BlackDigital.Domain
    // contract. The OpenAPI builder resolves the implementation to decide
    // whether an operation is secured.

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class ProductQuery
    {
        public string? Search { get; set; }
        public int Page { get; set; }
    }

    [Service("api/products", name: "Products", description: "Product management endpoints.")]
    public interface IProductService
    {
        [Action("", method: RestMethod.Get, name: "List Products", description: "Returns all products.")]
        Task<List<ProductDto>> GetAllAsync();

        [Action("{id}", method: RestMethod.Get)]
        Task<ProductDto?> GetByIdAsync([Path] int id);

        [Action("", method: RestMethod.Post)]
        Task<ProductDto> CreateAsync([Body] ProductDto product);

        [Action("{id}", method: RestMethod.Put)]
        Task<ProductDto> UpdateAsync([Path] int id, [Body] ProductDto product);

        [Action("{id}", method: RestMethod.Delete)]
        Task DeleteAsync([Path] int id);

        [Action("search", method: RestMethod.Get)]
        Task<List<ProductDto>> SearchAsync([Query] ProductQuery query);

        [Action("ping", method: RestMethod.Get)]
        Task PingAsync();
    }

    public class ProductService : IProductService
    {
        public Task<List<ProductDto>> GetAllAsync() => Task.FromResult(new List<ProductDto>());

        public Task<ProductDto?> GetByIdAsync(int id) => Task.FromResult<ProductDto?>(null);

        [Authorize]
        public Task<ProductDto> CreateAsync(ProductDto product) => Task.FromResult(product);

        public Task<ProductDto> UpdateAsync(int id, ProductDto product) => Task.FromResult(product);

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task<List<ProductDto>> SearchAsync(ProductQuery query) => Task.FromResult(new List<ProductDto>());

        public Task PingAsync() => Task.CompletedTask;
    }

    [Service("api/categories")]
    public interface ICategoryService
    {
        [Action("", method: RestMethod.Get)]
        Task<List<string>> GetCategoriesAsync();
    }

    public class CategoryService : ICategoryService
    {
        public Task<List<string>> GetCategoriesAsync() => Task.FromResult(new List<string>());
    }

    [Service("api/secure")]
    public interface ISecureService
    {
        [Action("data", method: RestMethod.Get)]
        Task<string> GetDataAsync();

        [Action("open", method: RestMethod.Get)]
        Task<string> GetOpenAsync();
    }

    // Class-level requirement applies to every action; the method-level one is
    // additional and is combined with OR semantics.
    [Authorize(Policy = "Admin")]
    public class SecureService : ISecureService
    {
        [Authorize(Policy = "Read")]
        public Task<string> GetDataAsync() => Task.FromResult("data");

        public Task<string> GetOpenAsync() => Task.FromResult("open");
    }

    // Entire service is deprecated — every operation should be marked deprecated.
    [Obsolete("Use IProductService instead.")]
    [Service("api/legacy", name: "Legacy", description: "Legacy endpoints.")]
    public interface ILegacyService
    {
        [Action("", method: RestMethod.Get)]
        Task<string> GetLegacyAsync();
    }

    // Only a single method is deprecated; the rest of the service stays active.
    [Service("api/mixed", name: "Mixed", description: "Mixed endpoints.")]
    public interface IMixedService
    {
        [Action("active", method: RestMethod.Get)]
        Task<string> GetActiveAsync();

        [Obsolete("Use GetActiveAsync instead.")]
        [Action("old", method: RestMethod.Get)]
        Task<string> GetOldAsync();
    }

    public class MixedService : IMixedService
    {
        public Task<string> GetActiveAsync() => Task.FromResult("active");

        public Task<string> GetOldAsync() => Task.FromResult("old");
    }
}
