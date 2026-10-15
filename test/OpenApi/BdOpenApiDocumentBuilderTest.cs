using BlackDigital.AspNet.OpenApi;
using BlackDigital.Rest;
using Microsoft.OpenApi;
using System.Net.Http;
using System.Reflection;

namespace BlackDigital.AspNet.Test.OpenApi
{
    public class BdOpenApiDocumentBuilderTest
    {
        private static BdOpenApiOptions CreateOptions()
        {
            var options = new BdOpenApiOptions();
            // The test assembly is "BlackDigital.AspNet.Test", which is excluded
            // by default (prefix "BlackDigital.AspNet"). Remove that exclusion
            // and explicitly include only the test assembly so other service
            // interfaces in the solution are not picked up.
            options.ExcludedAssemblyPrefixes.Remove("BlackDigital.AspNet");
            options.IncludedAssemblyPrefixes.Add("BlackDigital.AspNet.Test");
            return options;
        }

        private static BdOpenApiDocumentBuilder CreateBuilder() => new(CreateOptions());

        [Fact]
        public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BdOpenApiDocumentBuilder(null!));
        }

        [Fact]
        public void Title_ShouldReturnOptionsTitle()
        {
            var options = CreateOptions();
            options.Title = "My Test API";
            var builder = new BdOpenApiDocumentBuilder(options);

            Assert.Equal("My Test API", builder.Title);
        }

        [Fact]
        public void Version_ShouldReturnOptionsVersion()
        {
            var options = CreateOptions();
            options.Version = "v9.9";
            var builder = new BdOpenApiDocumentBuilder(options);

            Assert.Equal("v9.9", builder.Version);
        }

        [Fact]
        public void ServerUrl_WhenNotSet_ShouldReturnNull()
        {
            var builder = CreateBuilder();
            Assert.Null(builder.ServerUrl);
        }

        [Fact]
        public void ServerUrl_WhenSet_ShouldReturnValue()
        {
            var options = CreateOptions();
            options.ServerUrl = "https://api.example.com";
            var builder = new BdOpenApiDocumentBuilder(options);

            Assert.Equal("https://api.example.com", builder.ServerUrl);
        }

        [Fact]
        public void Build_ShouldSetInfoFromOptions()
        {
            var options = CreateOptions();
            options.Title = "Test API";
            options.Version = "v2";
            options.Description = "A test API description.";
            var builder = new BdOpenApiDocumentBuilder(options);

            var doc = builder.Build();

            Assert.Equal("Test API", doc.Info.Title);
            Assert.Equal("v2", doc.Info.Version);
            Assert.Equal("A test API description.", doc.Info.Description);
        }

        [Fact]
        public void Build_WhenServerUrlSet_ShouldAddServer()
        {
            var options = CreateOptions();
            options.ServerUrl = "https://api.example.com";
            var builder = new BdOpenApiDocumentBuilder(options);

            var doc = builder.Build();

            Assert.NotNull(doc.Servers);
            var server = Assert.Single(doc.Servers);
            Assert.Equal("https://api.example.com", server.Url);
        }

        [Fact]
        public void Build_WhenServerUrlNotSet_ShouldNotAddServer()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.True(doc.Servers == null || doc.Servers.Count == 0);
        }

        [Fact]
        public void Build_ShouldIncludeBearerSecurityScheme()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.NotNull(doc.Components.SecuritySchemes);
            Assert.True(doc.Components.SecuritySchemes.ContainsKey("Bearer"));
        }

        [Fact]
        public void Build_WhenApplyBearerAsGlobal_ShouldAddGlobalSecurityRequirement()
        {
            var options = CreateOptions();
            options.ApplyBearerAsGlobalSecurityRequirement = true;
            var builder = new BdOpenApiDocumentBuilder(options);

            var doc = builder.Build();

            Assert.NotNull(doc.Security);
            Assert.NotEmpty(doc.Security);
        }

        [Fact]
        public void Build_WhenApplyBearerAsGlobalFalse_ShouldNotAddGlobalSecurity()
        {
            var options = CreateOptions();
            options.ApplyBearerAsGlobalSecurityRequirement = false;
            var builder = new BdOpenApiDocumentBuilder(options);

            var doc = builder.Build();

            Assert.True(doc.Security == null || doc.Security.Count == 0);
        }

        [Fact]
        public void Build_ShouldDiscoverServiceInterfaces()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.NotNull(doc.Paths);
            // IProductService, ICategoryService, ISecureService should be present
            Assert.True(doc.Paths.Count > 0);
        }

        [Fact]
        public void Build_ShouldCreateTagsForEachService()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.NotNull(doc.Tags);
            Assert.NotEmpty(doc.Tags);
        }

        [Fact]
        public void Build_ShouldUseServiceNameAsTagName()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var productTag = doc.Tags.FirstOrDefault(t => t.Name == "Products");
            Assert.NotNull(productTag);
            Assert.Equal("Product management endpoints.", productTag.Description);
        }

        [Fact]
        public void Build_ShouldUseFriendlyGroupNameWhenNameNotSet()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            // ICategoryService has no name → "Categories" (friendly plural)
            var categoryTag = doc.Tags.FirstOrDefault(t => t.Name == "Categories");
            Assert.NotNull(categoryTag);
        }

        [Fact]
        public void Build_ShouldUseActionNameAsOperationSummary()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Get];
            Assert.Equal("List Products", op.Summary);
        }

        [Fact]
        public void Build_ShouldUseActionDescriptionAsOperationDescription()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Get];
            Assert.Equal("Returns all products.", op.Description);
        }

        [Fact]
        public void Build_WhenActionHasNoName_ShouldFormatMethodName()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products/{id}"];
            var op = path.Operations[HttpMethod.Get];
            Assert.Equal("Get By Id", op.Summary);
        }

        [Fact]
        public void Build_ShouldGeneratePathForGetAll()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.True(doc.Paths.ContainsKey("/api/products"));
        }

        [Fact]
        public void Build_ShouldGeneratePathForById()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.True(doc.Paths.ContainsKey("/api/products/{id}"));
        }

        [Fact]
        public void Build_ShouldGeneratePathForSearch()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.True(doc.Paths.ContainsKey("/api/products/search"));
        }

        [Fact]
        public void Build_ShouldHandlePathParameters()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products/{id}"];
            var op = path.Operations[HttpMethod.Get];
            Assert.NotNull(op.Parameters);
            var pathParam = op.Parameters.FirstOrDefault(p => p.Name == "id");
            Assert.NotNull(pathParam);
            Assert.Equal(ParameterLocation.Path, pathParam.In);
            Assert.True(pathParam.Required);
        }

        [Fact]
        public void Build_ShouldHandleBodyParameters()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Post];
            Assert.NotNull(op.RequestBody);
            Assert.True(op.RequestBody.Content.ContainsKey("application/json"));
        }

        [Fact]
        public void Build_ShouldExpandComplexQueryParameters()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products/search"];
            var op = path.Operations[HttpMethod.Get];
            Assert.NotNull(op.Parameters);
            var searchParam = op.Parameters.FirstOrDefault(p => p.Name == "Search");
            var pageParam = op.Parameters.FirstOrDefault(p => p.Name == "Page");
            Assert.NotNull(searchParam);
            Assert.Equal(ParameterLocation.Query, searchParam.In);
            Assert.NotNull(pageParam);
            Assert.Equal(ParameterLocation.Query, pageParam.In);
        }

        [Fact]
        public void Build_ForTaskOfT_ShouldReturn200WithContent()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Get];
            Assert.True(op.Responses.ContainsKey("200"));
            var response = op.Responses["200"];
            Assert.NotNull(response.Content);
            Assert.True(response.Content.ContainsKey("application/json"));
        }

        [Fact]
        public void Build_ForTask_ShouldReturn204()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products/ping"];
            var op = path.Operations[HttpMethod.Get];
            Assert.True(op.Responses.ContainsKey("204"));
        }

        [Fact]
        public void Build_ForDeleteTask_ShouldReturn204()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products/{id}"];
            var op = path.Operations[HttpMethod.Delete];
            Assert.True(op.Responses.ContainsKey("204"));
        }

        [Fact]
        public void Build_WhenActionRequiresAuth_ShouldAddSecurityRequirement()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Post];
            Assert.NotNull(op.Security);
            Assert.NotEmpty(op.Security);
        }

        [Fact]
        public void Build_WhenActionDoesNotRequireAuth_ShouldNotAddSecurity()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Get];
            Assert.True(op.Security == null || op.Security.Count == 0);
        }

        [Fact]
        public void Build_WhenServiceHasAuthorize_ShouldAddSecurityToAllOperations()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/secure/open"];
            var op = path.Operations[HttpMethod.Get];
            // ISecureService has authorize: true, so even "open" requires auth
            Assert.NotNull(op.Security);
            Assert.NotEmpty(op.Security);
        }

        [Fact]
        public void Build_WhenServiceHasPolicy_ShouldAddSecurityToAllOperations()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/secure/data"];
            var op = path.Operations[HttpMethod.Get];
            // ISecureService has policy "Admin", and data action has policy "Read"
            Assert.NotNull(op.Security);
            Assert.NotEmpty(op.Security);
        }

        [Fact]
        public void Build_ShouldRegisterComponentSchemas()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            Assert.NotNull(doc.Components.Schemas);
            // ProductDto should be registered as a component schema
            Assert.NotEmpty(doc.Components.Schemas);
        }

        [Fact]
        public void Build_ShouldSetOperationId()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            var op = path.Operations[HttpMethod.Get];
            Assert.Equal("IProductService_GetAllAsync", op.OperationId);
        }

        [Fact]
        public void Build_ShouldSupportMultipleHttpMethodsOnSamePath()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/products"];
            Assert.True(path.Operations.ContainsKey(HttpMethod.Get));
            Assert.True(path.Operations.ContainsKey(HttpMethod.Post));
        }

        // --- [Obsolete] deprecation tests ---

        [Fact]
        public void Build_WhenServiceInterfaceIsObsolete_ShouldMarkAllOperationsAsDeprecated()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var path = doc.Paths["/api/legacy"];
            var op = path.Operations[HttpMethod.Get];

            Assert.True(op.Deprecated);
        }

        [Fact]
        public void Build_WhenServiceInterfaceIsObsolete_ShouldAppendDeprecatedToTagDescription()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var legacyTag = doc.Tags.FirstOrDefault(t => t.Name == "Legacy");
            Assert.NotNull(legacyTag);
            Assert.Contains("deprecated", legacyTag.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Use IProductService instead.", legacyTag.Description);
        }

        [Fact]
        public void Build_WhenOnlyMethodIsObsolete_ShouldMarkOnlyThatOperationAsDeprecated()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            // Active method — not deprecated.
            var activeOp = doc.Paths["/api/mixed/active"].Operations[HttpMethod.Get];
            Assert.False(activeOp.Deprecated);

            // Obsolete method — deprecated.
            var oldOp = doc.Paths["/api/mixed/old"].Operations[HttpMethod.Get];
            Assert.True(oldOp.Deprecated);
        }

        [Fact]
        public void Build_WhenMethodIsObsoleteWithMessage_ShouldAppendMessageToDescription()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var oldOp = doc.Paths["/api/mixed/old"].Operations[HttpMethod.Get];
            Assert.Contains("Use GetActiveAsync instead.", oldOp.Description);
        }

        [Fact]
        public void Build_WhenNeitherServiceNorMethodIsObsolete_ShouldNotMarkOperationAsDeprecated()
        {
            var builder = CreateBuilder();

            var doc = builder.Build();

            var op = doc.Paths["/api/products"].Operations[HttpMethod.Get];
            Assert.False(op.Deprecated);
        }

        // --- Tests for private helpers via reflection ---

        [Theory]
        [InlineData("GetCategoriesAsync", "Get Categories")]
        [InlineData("GetCategoryByIdAsync", "Get Category By Id")]
        [InlineData("ExpireOldInvitationsAsync", "Expire Old Invitations")]
        [InlineData("Get", "Get")]
        [InlineData("", "")]
        [InlineData("APIKey", "A P I Key")]
        public void FormatMethodName_ShouldSplitPascalCase(string input, string expected)
        {
            var method = typeof(BdOpenApiDocumentBuilder).GetMethod(
                "FormatMethodName",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var result = method.Invoke(null, new object[] { input });

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(typeof(IProductService), "Products")]
        [InlineData(typeof(ICategoryService), "Categories")]
        public void FriendlyGroupName_ShouldPluralizeServiceName(Type serviceType, string expected)
        {
            var method = typeof(BdOpenApiDocumentBuilder).GetMethod(
                "FriendlyGroupName",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var result = method.Invoke(null, new object[] { serviceType });

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetCandidateAssemblies_WhenNoInclude_ShouldReturnNonExcludedAssemblies()
        {
            var options = new BdOpenApiOptions();
            options.IncludedAssemblyPrefixes.Clear();
            var builder = new BdOpenApiDocumentBuilder(options);

            var method = typeof(BdOpenApiDocumentBuilder).GetMethod(
                "GetCandidateAssemblies",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            var result = (IEnumerable<System.Reflection.Assembly>)method.Invoke(builder, null)!;

            // Should not include System.* or Microsoft.* assemblies
            var names = result.Select(a => a.GetName().Name).ToList();
            Assert.DoesNotContain(names, n => n?.StartsWith("System") == true);
            Assert.DoesNotContain(names, n => n?.StartsWith("Microsoft") == true);
        }

        [Fact]
        public void GetCandidateAssemblies_WithInclude_ShouldOnlyReturnMatchingAssemblies()
        {
            var options = new BdOpenApiOptions();
            options.ExcludedAssemblyPrefixes.Clear();
            options.IncludedAssemblyPrefixes.Add("BlackDigital.AspNet.Test");
            var builder = new BdOpenApiDocumentBuilder(options);

            var method = typeof(BdOpenApiDocumentBuilder).GetMethod(
                "GetCandidateAssemblies",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            var result = (IEnumerable<System.Reflection.Assembly>)method.Invoke(builder, null)!;

            var names = result.Select(a => a.GetName().Name).ToList();
            Assert.All(names, n => Assert.StartsWith("BlackDigital.AspNet.Test", n));
        }
    }
}
