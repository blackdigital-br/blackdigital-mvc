using BlackDigital.Mvc.Rest;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlackDigital.Mvc.Test.Rest
{
    // Interfaces e classes mock para testes
    public interface ITestService
    {
        string GetData();
    }

    public class TestServiceImplementation : ITestService
    {
        public string GetData() => "Test Data";
    }

    public interface IAnotherService
    {
        int GetNumber();
    }

    public class AnotherServiceImplementation : IAnotherService
    {
        public int GetNumber() => 42;
    }

    public class RestServiceBuilderTest
    {
        [Fact]
        public void Constructor_WhenServicesIsValid_ShouldInitializeSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var builder = new RestServiceBuilder(services);

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void AddService_ShouldAddServiceWithScopedLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);

            // Act
            var result = builder.AddService<ITestService, TestServiceImplementation>();

            // Assert
            Assert.Same(builder, result); // Verifica fluent interface
            Assert.Single(services);
            var serviceDescriptor = services.First();
            Assert.Equal(typeof(ITestService), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(TestServiceImplementation), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        }

        [Fact]
        public void AddSingletonService_ShouldAddServiceWithSingletonLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);

            // Act
            var result = builder.AddSingletonService<ITestService, TestServiceImplementation>();

            // Assert
            Assert.Same(builder, result); // Verifica fluent interface
            Assert.Single(services);
            var serviceDescriptor = services.First();
            Assert.Equal(typeof(ITestService), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(TestServiceImplementation), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [Fact]
        public void AddTransientService_ShouldAddServiceWithTransientLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);

            // Act
            var result = builder.AddTransientService<ITestService, TestServiceImplementation>();

            // Assert
            Assert.Same(builder, result); // Verifica fluent interface
            Assert.Single(services);
            var serviceDescriptor = services.First();
            Assert.Equal(typeof(ITestService), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(TestServiceImplementation), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
        }

        [Fact]
        public void AddServiceWithFactory_ShouldAddServiceWithFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);
            Func<IServiceProvider, ITestService> factory = provider => new TestServiceImplementation();

            // Act
            var result = builder.AddService(factory);

            // Assert
            Assert.Same(builder, result); // Verifica fluent interface
            Assert.Single(services);
            var serviceDescriptor = services.First();
            Assert.Equal(typeof(ITestService), serviceDescriptor.ServiceType);
            Assert.Equal(factory, serviceDescriptor.ImplementationFactory);
            Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        }

        [Fact]
        public void FluentInterface_ShouldAllowMethodChaining()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);
            Func<IServiceProvider, IAnotherService> factory = provider => new AnotherServiceImplementation();

            // Act
            var result = builder
                .AddService<ITestService, TestServiceImplementation>()
                .AddSingletonService<IAnotherService, AnotherServiceImplementation>()
                .AddTransientService<ITestService, TestServiceImplementation>()
                .AddService(factory);

            // Assert
            Assert.Same(builder, result);
            Assert.Equal(4, services.Count);
        }

        [Fact]
        public void MultipleServices_ShouldAddAllServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);
            Func<IServiceProvider, IAnotherService> factory = provider => new AnotherServiceImplementation();

            // Act
            builder
                .AddService<ITestService, TestServiceImplementation>()
                .AddSingletonService<IAnotherService, AnotherServiceImplementation>()
                .AddTransientService<ITestService, TestServiceImplementation>()
                .AddService(factory);

            // Assert
            Assert.Equal(4, services.Count);

            // Verifica primeiro serviço (Scoped)
            var firstService = services[0];
            Assert.Equal(typeof(ITestService), firstService.ServiceType);
            Assert.Equal(typeof(TestServiceImplementation), firstService.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, firstService.Lifetime);

            // Verifica segundo serviço (Singleton)
            var secondService = services[1];
            Assert.Equal(typeof(IAnotherService), secondService.ServiceType);
            Assert.Equal(typeof(AnotherServiceImplementation), secondService.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, secondService.Lifetime);

            // Verifica terceiro serviço (Transient)
            var thirdService = services[2];
            Assert.Equal(typeof(ITestService), thirdService.ServiceType);
            Assert.Equal(typeof(TestServiceImplementation), thirdService.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, thirdService.Lifetime);

            // Verifica quarto serviço (Factory)
            var fourthService = services[3];
            Assert.Equal(typeof(IAnotherService), fourthService.ServiceType);
            Assert.Equal(factory, fourthService.ImplementationFactory);
            Assert.Equal(ServiceLifetime.Scoped, fourthService.Lifetime);
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Transient)]
        public void ServiceLifetimes_ShouldBeCorrectlySet(ServiceLifetime expectedLifetime)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new RestServiceBuilder(services);

            // Act
            switch (expectedLifetime)
            {
                case ServiceLifetime.Scoped:
                    builder.AddService<ITestService, TestServiceImplementation>();
                    break;
                case ServiceLifetime.Singleton:
                    builder.AddSingletonService<ITestService, TestServiceImplementation>();
                    break;
                case ServiceLifetime.Transient:
                    builder.AddTransientService<ITestService, TestServiceImplementation>();
                    break;
            }

            // Assert
            Assert.Single(services);
            Assert.Equal(expectedLifetime, services.First().Lifetime);
        }
    }
}