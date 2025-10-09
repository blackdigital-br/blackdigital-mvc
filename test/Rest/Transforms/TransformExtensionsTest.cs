using BlackDigital.AspNet.Rest.Trasnforms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlackDigital.AspNet.Test.Rest.Transforms
{
    // Test class for testing abstract classes
    public class ExtensionsTestTransformRule : TransformRule
    {
        public override object? Transform(object? value)
        {
            return value?.ToString()?.ToUpper();
        }
    }

    public class TransformExtensionsTest
    {
        [Fact]
        public void AddTransform_ShouldRegisterTransformBuilderAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTransform(builder => builder);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var builder1 = serviceProvider.GetService<TransformBuilder>();
            var builder2 = serviceProvider.GetService<TransformBuilder>();

            Assert.NotNull(builder1);
            Assert.NotNull(builder2);
            Assert.Same(builder1, builder2); // Should be the same instance (singleton)
        }

        [Fact]
        public void AddTransform_ShouldRegisterTransformManagerAsScoped()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTransform(builder => builder);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            using var scope1 = serviceProvider.CreateScope();
            using var scope2 = serviceProvider.CreateScope();
            
            var manager1a = scope1.ServiceProvider.GetService<TransformManager>();
            var manager1b = scope1.ServiceProvider.GetService<TransformManager>();
            var manager2 = scope2.ServiceProvider.GetService<TransformManager>();

            Assert.NotNull(manager1a);
            Assert.NotNull(manager1b);
            Assert.NotNull(manager2);
            Assert.Same(manager1a, manager1b); // Same instance within the same scope
            Assert.NotSame(manager1a, manager2); // Different instances across different scopes
        }

        [Fact]
        public void AddTransform_ShouldAllowConfigurationOfTransformBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationCalled = false;

            // Act
            services.AddTransform(builder =>
            {
                configurationCalled = true;
                builder.AddRule("test", "1.0", new Mock<ITransformRule>().Object);
                return builder;
            });

            // Assert
            Assert.True(configurationCalled);
            
            var serviceProvider = services.BuildServiceProvider();
            var transformBuilder = serviceProvider.GetService<TransformBuilder>();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformBuilder);
            Assert.NotNull(transformManager);
            Assert.True(transformManager.HasRule("test", "1.0"));
        }

        [Fact]
        public void AddTransform_WithEmptyConfiguration_ShouldNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            var exception = Record.Exception(() => services.AddTransform(builder => builder));
            Assert.Null(exception);
            
            var serviceProvider = services.BuildServiceProvider();
            var transformBuilder = serviceProvider.GetService<TransformBuilder>();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformBuilder);
            Assert.NotNull(transformManager);
        }

        [Fact]
        public void AddTransform_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTransform(builder => builder);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddTransform_WithConfiguration_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTransform(builder => builder);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddTransform_ShouldAllowMultipleRulesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;

            // Act
            services.AddTransform(builder =>
            {
                builder.AddRule("test1", "1.0", rule1);
                builder.AddRule("test2", "1.0", rule2);
                return builder;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformManager);
            Assert.True(transformManager.HasRule("test1", "1.0"));
            Assert.True(transformManager.HasRule("test2", "1.0"));
        }

        [Fact]
        public void AddTransform_ShouldAllowComplexRuleConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var inputRule = new Mock<ITransformRule>().Object;
            var outputRule = new Mock<ITransformRule>().Object;
            var bothRule = new Mock<ITransformRule>().Object;

            // Act
            services.AddTransform(builder =>
            {
                builder.AddInputRule("test", "1.0", inputRule);
                builder.AddOutputRule("test", "1.0", outputRule);
                builder.AddInputAndOutputRule("test", "1.0", bothRule);
                return builder;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformManager);
            Assert.True(transformManager.HasRule("test", "1.0", TransformDirection.Input));
            Assert.True(transformManager.HasRule("test", "1.0", TransformDirection.Output));
            Assert.True(transformManager.HasRule("test", "1.0", TransformDirection.Both));
        }

        [Fact]
        public void AddTransform_ShouldWorkWithGenericRules()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTransient<ExtensionsTestTransformRule>(); // Register the rule type in DI
            services.AddTransform(builder =>
            {
                builder.AddRule<ExtensionsTestTransformRule>("test", "1.0");
                builder.AddInputRule<ExtensionsTestTransformRule>("test2", "1.0");
                builder.AddOutputRule<ExtensionsTestTransformRule>("test3", "1.0");
                return builder;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformManager);
            
            // Debug: Check if rules exist with correct directions
            var hasTest = transformManager.HasRule("test", "1.0", TransformDirection.Input); // AddRule defaults to Input
            var hasTest2 = transformManager.HasRule("test2", "1.0", TransformDirection.Input); // AddInputRule
            var hasTest3 = transformManager.HasRule("test3", "1.0", TransformDirection.Output); // AddOutputRule
            
            Assert.True(hasTest, "Rule 'test' should exist with Input direction");
            Assert.True(hasTest2, "Rule 'test2' should exist with Input direction");
            Assert.True(hasTest3, "Rule 'test3' should exist with Output direction");
        }

        [Fact]
        public void AddTransform_ShouldWorkWithTypeBasedRules()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTransient<ExtensionsTestTransformRule>(); // Register the rule type in DI
            services.AddTransform(builder =>
            {
                builder.AddRule("test", "1.0", typeof(ExtensionsTestTransformRule));
                return builder;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformManager);
            Assert.True(transformManager.HasRule("test", "1.0"));
        }

        [Fact]
        public void AddTransform_CalledMultipleTimes_ShouldRegisterMultipleBuilders()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTransform(builder => builder);
            services.AddTransform(builder => builder);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var builders = serviceProvider.GetServices<TransformBuilder>().ToList();
            var managers = serviceProvider.GetServices<TransformManager>().ToList();

            // Each call to AddTransform registers a new TransformBuilder and TransformManager
            Assert.Equal(2, builders.Count);
            Assert.Equal(2, managers.Count);
        }

        [Fact]
        public void AddTransform_WithMultipleConfigurations_ShouldUseLastRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;

            // Act
            services.AddTransform(builder => builder.AddRule("test1", "1.0", rule1));
            services.AddTransform(builder => builder.AddRule("test2", "1.0", rule2));

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var transformManager = serviceProvider.GetService<TransformManager>();
            
            Assert.NotNull(transformManager);
            // Only the last registration is used by TransformManager
            Assert.False(transformManager.HasRule("test1", "1.0"));
            Assert.True(transformManager.HasRule("test2", "1.0"));
        }
    }
}
