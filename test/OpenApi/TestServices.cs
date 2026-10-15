using BlackDigital.Rest;

namespace BlackDigital.AspNet.Test.OpenApi
{
    // Test service interfaces used by BdOpenApiDocumentBuilderTest.
    // These are scanned from the test assembly when the builder's
    // assembly prefix filter is configured to include "BlackDigital.AspNet.Test".

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

    [Service("api/products", authorize: false, name: "Products", description: "Product management endpoints.")]
    public interface IProductService
    {
        [Action("", method: RestMethod.Get, authorize: false, name: "List Products", description: "Returns all products.")]
        Task<List<ProductDto>> GetAllAsync();

        [Action("{id}", method: RestMethod.Get, authorize: false)]
        Task<ProductDto?> GetByIdAsync([Path] int id);

        [Action("", method: RestMethod.Post, authorize: true)]
        Task<ProductDto> CreateAsync([Body] ProductDto product);

        [Action("{id}", method: RestMethod.Put, authorize: false)]
        Task<ProductDto> UpdateAsync([Path] int id, [Body] ProductDto product);

        [Action("{id}", method: RestMethod.Delete, authorize: false)]
        Task DeleteAsync([Path] int id);

        [Action("search", method: RestMethod.Get, authorize: false)]
        Task<List<ProductDto>> SearchAsync([Query] ProductQuery query);

        [Action("ping", method: RestMethod.Get, authorize: false)]
        Task PingAsync();
    }

    [Service("api/categories", authorize: false)]
    public interface ICategoryService
    {
        [Action("", method: RestMethod.Get, authorize: false)]
        Task<List<string>> GetCategoriesAsync();
    }

    [Service("api/secure", authorize: true, policy: "Admin")]
    public interface ISecureService
    {
        [Action("data", method: RestMethod.Get, authorize: false, policy: "Read")]
        Task<string> GetDataAsync();

        [Action("open", method: RestMethod.Get, authorize: false)]
        Task<string> GetOpenAsync();
    }
}
