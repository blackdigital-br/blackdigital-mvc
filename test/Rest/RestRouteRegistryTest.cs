using BlackDigital.AspNet.Rest;
using BlackDigital.Rest;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BlackDigital.AspNet.Test.Rest
{
    [Service("api/registrytest")]
    public interface IRegistryTestService
    {
        [Action("", method: RestMethod.Get)]
        System.Threading.Tasks.Task<string> GetAllAsync();

        [Action("{id}", method: RestMethod.Get)]
        System.Threading.Tasks.Task<string> GetByIdAsync([Path] int id);

        [Action("search", method: RestMethod.Get)]
        System.Threading.Tasks.Task<string> SearchAsync();
    }

    public class RegistryTestService : IRegistryTestService
    {
        public System.Threading.Tasks.Task<string> GetAllAsync() => System.Threading.Tasks.Task.FromResult("all");
        public System.Threading.Tasks.Task<string> GetByIdAsync(int id) => System.Threading.Tasks.Task.FromResult(id.ToString());
        public System.Threading.Tasks.Task<string> SearchAsync() => System.Threading.Tasks.Task.FromResult("search");
    }

    public class RestRouteRegistryTest
    {
        private static RestRouteRegistry CreateRegistry()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRegistryTestService, RegistryTestService>();
            return new RestRouteRegistry(services);
        }

        [Fact]
        public void GetStaticRoutes_WhenRouteHasNoParameters_ShouldBeInDictionary()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var staticRoutes = registry.GetStaticRoutes("GET");

            // Assert
            Assert.True(staticRoutes.ContainsKey("/api/registrytest"));
            Assert.True(staticRoutes.ContainsKey("/api/registrytest/search"));
        }

        [Fact]
        public void GetStaticRoutes_WhenRouteHasParameters_ShouldNotBeInDictionary()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var staticRoutes = registry.GetStaticRoutes("GET");

            // Assert
            Assert.False(staticRoutes.ContainsKey("/api/registrytest/{id}"));
        }

        [Fact]
        public void GetParameterizedRoutes_WhenRouteHasParameters_ShouldBeGroupedBySegmentCount()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var parameterized = registry.GetParameterizedRoutes("GET");

            // Assert
            // /api/registrytest/{id} -> segments: api, registrytest, {id} = 3
            Assert.True(parameterized.ContainsKey(3));
            Assert.Single(parameterized[3]);
        }

        [Fact]
        public void GetRoutesForMethod_ShouldReturnAllRoutesForMethod()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var routes = registry.GetRoutesForMethod("GET");

            // Assert
            Assert.Equal(3, routes.Count);
        }

        [Fact]
        public void GetRoutesForMethod_WhenNoRoutes_ShouldReturnEmpty()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var routes = registry.GetRoutesForMethod("DELETE");

            // Assert
            Assert.Empty(routes);
        }

        [Fact]
        public void RestRouteEntry_WhenPatternHasParameters_ShouldSetHasParametersTrue()
        {
            // Arrange
            var service = ServiceDescriptor.Singleton(typeof(object), new object());
            var method = typeof(object).GetMethod(nameof(object.ToString))!;
            var entry = new RestRouteEntry("/api/test/{id}", service, method);

            // Act & Assert
            Assert.True(entry.HasParameters);
            Assert.Equal(new[] { "api", "test", "{id}" }, entry.Segments);
        }

        [Fact]
        public void RestRouteEntry_WhenPatternHasNoParameters_ShouldSetHasParametersFalse()
        {
            // Arrange
            var service = ServiceDescriptor.Singleton(typeof(object), new object());
            var method = typeof(object).GetMethod(nameof(object.ToString))!;
            var entry = new RestRouteEntry("/api/test", service, method);

            // Act & Assert
            Assert.False(entry.HasParameters);
            Assert.Equal(new[] { "api", "test" }, entry.Segments);
        }
    }
}
