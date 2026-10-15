using BlackDigital.AspNet.OpenApi;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.Test.OpenApi
{
    public class BdOpenApiOptionsTest
    {
        [Fact]
        public void Constructor_ShouldHaveDefaultValues()
        {
            var options = new BdOpenApiOptions();

            Assert.Equal("API", options.Title);
            Assert.Equal("v1", options.Version);
            Assert.Null(options.Description);
            Assert.Null(options.Contact);
            Assert.Null(options.License);
            Assert.Null(options.ServerUrl);
            Assert.Equal("v1", options.DocumentName);
            Assert.True(options.Enabled);
            Assert.True(options.ApplyBearerAsGlobalSecurityRequirement);
            Assert.Null(options.ScalarTitle);
            Assert.Equal("/openapi/{documentName}.json", options.OpenApiRoutePattern);
        }

        [Fact]
        public void IncludedAssemblyPrefixes_ShouldStartEmpty()
        {
            var options = new BdOpenApiOptions();

            Assert.Empty(options.IncludedAssemblyPrefixes);
        }

        [Fact]
        public void ExcludedAssemblyPrefixes_ShouldContainDefaultValues()
        {
            var options = new BdOpenApiOptions();

            Assert.Contains("System", options.ExcludedAssemblyPrefixes);
            Assert.Contains("Microsoft", options.ExcludedAssemblyPrefixes);
            Assert.Contains("BlackDigital.AspNet", options.ExcludedAssemblyPrefixes);
            Assert.Contains("BlackDigital.Rest", options.ExcludedAssemblyPrefixes);
        }

        [Fact]
        public void SecuritySchemes_ShouldContainBearerByDefault()
        {
            var options = new BdOpenApiOptions();

            Assert.True(options.SecuritySchemes.ContainsKey("Bearer"));
            var bearer = options.SecuritySchemes["Bearer"];
            Assert.Equal(SecuritySchemeType.Http, bearer.Type);
            Assert.Equal("Bearer", bearer.Scheme);
            Assert.Equal("JWT", bearer.BearerFormat);
        }

        [Fact]
        public void PreferredSecuritySchemes_ShouldContainBearerByDefault()
        {
            var options = new BdOpenApiOptions();

            Assert.Single(options.PreferredSecuritySchemes);
            Assert.Equal("Bearer", options.PreferredSecuritySchemes[0]);
        }

        [Fact]
        public void Clone_ShouldCopyScalarProperties()
        {
            var options = new BdOpenApiOptions
            {
                Title = "My API",
                Version = "v2",
                Description = "A test API",
                ServerUrl = "https://api.example.com",
                DocumentName = "docs",
                Enabled = false,
                ApplyBearerAsGlobalSecurityRequirement = false,
                ScalarTitle = "Scalar",
                OpenApiRoutePattern = "/api/docs/{documentName}"
            };

            var clone = options.Clone();

            Assert.Equal("My API", clone.Title);
            Assert.Equal("v2", clone.Version);
            Assert.Equal("A test API", clone.Description);
            Assert.Equal("https://api.example.com", clone.ServerUrl);
            Assert.Equal("docs", clone.DocumentName);
            Assert.False(clone.Enabled);
            Assert.False(clone.ApplyBearerAsGlobalSecurityRequirement);
            Assert.Equal("Scalar", clone.ScalarTitle);
            Assert.Equal("/api/docs/{documentName}", clone.OpenApiRoutePattern);
        }

        [Fact]
        public void Clone_ShouldCopyIncludedAssemblyPrefixes()
        {
            var options = new BdOpenApiOptions();
            options.IncludedAssemblyPrefixes.Add("MyProject");

            var clone = options.Clone();

            Assert.Contains("MyProject", clone.IncludedAssemblyPrefixes);
            Assert.NotSame(options.IncludedAssemblyPrefixes, clone.IncludedAssemblyPrefixes);
        }

        [Fact]
        public void Clone_ShouldCopyExcludedAssemblyPrefixes()
        {
            var options = new BdOpenApiOptions();
            options.ExcludedAssemblyPrefixes.Add("CustomExcluded");

            var clone = options.Clone();

            Assert.Contains("CustomExcluded", clone.ExcludedAssemblyPrefixes);
            Assert.NotSame(options.ExcludedAssemblyPrefixes, clone.ExcludedAssemblyPrefixes);
        }

        [Fact]
        public void Clone_ShouldDeepCopySecuritySchemes()
        {
            var options = new BdOpenApiOptions();

            var clone = options.Clone();

            Assert.NotSame(options.SecuritySchemes, clone.SecuritySchemes);
            Assert.Contains("Bearer", clone.SecuritySchemes);
            // Mutating clone should not affect original
            clone.SecuritySchemes["Bearer"].Scheme = "Basic";
            Assert.Equal("Bearer", options.SecuritySchemes["Bearer"].Scheme);
        }

        [Fact]
        public void Clone_ShouldCopyPreferredSecuritySchemes()
        {
            var options = new BdOpenApiOptions();

            var clone = options.Clone();

            Assert.NotSame(options.PreferredSecuritySchemes, clone.PreferredSecuritySchemes);
            Assert.Equal(options.PreferredSecuritySchemes, clone.PreferredSecuritySchemes);
        }
    }
}
