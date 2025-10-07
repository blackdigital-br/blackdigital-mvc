using BlackDigital.Mvc.Binder;
using BlackDigital.Mvc.Constraint;
using BlackDigital.Mvc.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace BlackDigital.Mvc.Test.Rest
{
    public class RestHelperTest
    {
        [Fact]
        public void AddRestMvcOptions_ShouldAddControllersAndConfigureRouteOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRestMvcOptions();

            // Assert
            Assert.Same(services, result); // Verifica fluent interface

            // Verifica se os controllers foram adicionados
            var serviceProvider = services.BuildServiceProvider();
            var mvcOptions = serviceProvider.GetService<IOptions<MvcOptions>>();
            Assert.NotNull(mvcOptions);

            // Verifica se as RouteOptions foram configuradas
            var routeOptions = serviceProvider.GetService<IOptions<RouteOptions>>();
            Assert.NotNull(routeOptions);
            Assert.True(routeOptions.Value.ConstraintMap.ContainsKey("id"));
            Assert.Equal(typeof(IdConstraint), routeOptions.Value.ConstraintMap["id"]);
        }

        [Fact]
        public void AddRestMvcOptions_ShouldAddIdConstraintToConstraintMap()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRestMvcOptions();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var routeOptions = serviceProvider.GetService<IOptions<RouteOptions>>();
            
            Assert.NotNull(routeOptions);
            Assert.True(routeOptions.Value.ConstraintMap.ContainsKey("id"));
            Assert.Equal(typeof(IdConstraint), routeOptions.Value.ConstraintMap["id"]);
        }

        [Fact]
        public void AddDefaultOptions_ShouldAddIdModelBinderProvider()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var initialProviderCount = mvcOptions.ModelBinderProviders.Count;

            // Act
            var result = mvcOptions.AddDefaultOptions();

            // Assert
            Assert.Same(mvcOptions, result); // Verifica fluent interface
            Assert.Equal(initialProviderCount + 1, mvcOptions.ModelBinderProviders.Count);
            
            // Verifica se o IdModelBinderProvider foi inserido na posição 0
            Assert.IsType<IdModelBinderProvider>(mvcOptions.ModelBinderProviders[0]);
        }

        [Fact]
        public void AddDefaultOptions_ShouldInsertIdModelBinderProviderAtPosition0()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            
            // Adiciona um provider mock para testar a inserção na posição 0
            var mockProvider = new Mock<IModelBinderProvider>();
            mvcOptions.ModelBinderProviders.Add(mockProvider.Object);
            
            var initialProviderCount = mvcOptions.ModelBinderProviders.Count;

            // Act
            mvcOptions.AddDefaultOptions();

            // Assert
            Assert.Equal(initialProviderCount + 1, mvcOptions.ModelBinderProviders.Count);
            Assert.IsType<IdModelBinderProvider>(mvcOptions.ModelBinderProviders[0]);
            
            // Verifica se o provider existente foi movido para a posição seguinte
            Assert.Same(mockProvider.Object, mvcOptions.ModelBinderProviders[1]);
        }

        [Fact]
        public void AddRestMvcOptions_ShouldReturnSameServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRestMvcOptions();

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddDefaultOptions_ShouldReturnSameMvcOptions()
        {
            // Arrange
            var mvcOptions = new MvcOptions();

            // Act
            var result = mvcOptions.AddDefaultOptions();

            // Assert
            Assert.Same(mvcOptions, result);
        }
    }
}