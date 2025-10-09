using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using BlackDigital.AspNet.Rest;

namespace BlackDigital.AspNet.Test.Rest
{
    public class RestMiddlewareExtensionsTest
    {
        [Fact]
        public void AddRestServices_WithValidBuilder_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            Func<RestServiceBuilder, RestServiceBuilder> builderFunc = builder => builder;

            // Act
            var result = services.AddRestServices(builderFunc);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddRestServices_WithBuilderConfiguration_ShouldExecuteBuilderFunction()
        {
            // Arrange
            var services = new ServiceCollection();
            bool builderFunctionCalled = false;
            
            Func<RestServiceBuilder, RestServiceBuilder> builderFunc = builder =>
            {
                builderFunctionCalled = true;
                return builder;
            };

            // Act
            services.AddRestServices(builderFunc);

            // Assert
            Assert.True(builderFunctionCalled);
        }

        [Fact]
        public void AddRestServices_FluentInterface_ShouldReturnIServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            Func<RestServiceBuilder, RestServiceBuilder> builderFunc = builder => builder;

            // Act
            var result = services.AddRestServices(builderFunc);

            // Assert
            Assert.IsType<ServiceCollection>(result);
            Assert.Same(services, result);
        }

        [Fact]
        public void AddRestServices_WithComplexBuilderConfiguration_ShouldExecuteCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            int configurationSteps = 0;
            
            Func<RestServiceBuilder, RestServiceBuilder> builderFunc = builder =>
            {
                configurationSteps++;
                // Simular configurações mais complexas
                return builder;
            };

            // Act
            var result = services.AddRestServices(builderFunc);

            // Assert
            Assert.Same(services, result);
            Assert.Equal(1, configurationSteps);
        }

        [Fact]
        public void AddRestServices_ShouldCreateRestServiceBuilderWithCorrectServices()
        {
            // Arrange
            var services = new ServiceCollection();
            RestServiceBuilder capturedBuilder = null;
            
            Func<RestServiceBuilder, RestServiceBuilder> builderFunc = builder =>
            {
                capturedBuilder = builder;
                return builder;
            };

            // Act
            services.AddRestServices(builderFunc);

            // Assert
            Assert.NotNull(capturedBuilder);
        }
    }
}
