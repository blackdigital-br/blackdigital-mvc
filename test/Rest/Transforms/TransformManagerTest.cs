using BlackDigital.Mvc.Rest.Trasnforms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlackDigital.Mvc.Test.Rest.Transforms
{
    public class TransformManagerTest
    {
        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            return services.BuildServiceProvider();
        }

        [Fact]
        public void Constructor_ShouldLoadRulesFromBuilder()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            builder.AddRule(key, rule);
            var serviceProvider = CreateServiceProvider();

            // Act
            var manager = new TransformManager(builder, serviceProvider);

            // Assert
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void Constructor_ShouldCreateRuleInstancesFromTypes()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            builder.AddRule(key, typeof(TestTransformRule));
            var serviceProvider = CreateServiceProvider();

            // Act
            var manager = new TransformManager(builder, serviceProvider);

            // Assert
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void Constructor_ShouldPrioritizeRuleInstancesOverTypes()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            builder.AddRule(key, rule);
            builder.AddRule(key, typeof(TestTransformRule)); // This should be ignored
            var serviceProvider = CreateServiceProvider();

            // Act
            var manager = new TransformManager(builder, serviceProvider);

            // Assert
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void HasRule_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            builder.AddRule(key, rule);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);

            // Act
            var result = manager.HasRule(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRule_WithNonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            var builder = new TransformBuilder();
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.HasRule(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasRule_WithStringParameters_ShouldReturnCorrectResult()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            builder.AddRule("test", "1.0", rule);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);

            // Act
            var result = manager.HasRule("test", "1.0");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetRequiredRules_WithNoMatchingRules_ShouldReturnNull()
        {
            // Arrange
            var builder = new TransformBuilder();
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetRequiredRules(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRequiredRules_WithMatchingRules_ShouldReturnOrderedRules()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;
            var rule3 = new Mock<ITransformRule>().Object;

            // Add rules with different versions
            builder.AddRule("test", "1.1", rule1, TransformDirection.Input);
            builder.AddRule("test", "1.3", rule2, TransformDirection.Input);
            builder.AddRule("test", "1.2", rule3, TransformDirection.Input);

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetRequiredRules(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // For Input direction, should be ordered ascending by version
            Assert.Equal(rule1, result[0]); // 1.1
            Assert.Equal(rule3, result[1]); // 1.2
            Assert.Equal(rule2, result[2]); // 1.3
        }

        [Fact]
        public void GetRequiredRules_WithOutputDirection_ShouldReturnDescendingOrder()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;
            var rule3 = new Mock<ITransformRule>().Object;

            // Add rules with different versions
            builder.AddRule("test", "1.1", rule1, TransformDirection.Output);
            builder.AddRule("test", "1.3", rule2, TransformDirection.Output);
            builder.AddRule("test", "1.2", rule3, TransformDirection.Output);

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Output);

            // Act
            var result = manager.GetRequiredRules(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // For Output direction, should be ordered descending by version
            Assert.Equal(rule2, result[0]); // 1.3
            Assert.Equal(rule3, result[1]); // 1.2
            Assert.Equal(rule1, result[2]); // 1.1
        }

        [Fact]
        public void GetRequiredRules_WithStringParameters_ShouldWork()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            builder.AddRule("test", "1.1", rule, TransformDirection.Input);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);

            // Act
            var result = manager.GetRequiredRules("test", "1.0", TransformDirection.Input);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(rule, result[0]);
        }

        [Fact]
        public void GetFirstInputType_WithNoRules_ShouldReturnNull()
        {
            // Arrange
            var builder = new TransformBuilder();
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetFirstInputType(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFirstInputType_WithRules_ShouldReturnFirstRuleInputType()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>();
            rule.Setup(r => r.InputType).Returns(typeof(string));
            builder.AddRule("test", "1.1", rule.Object, TransformDirection.Input);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetFirstInputType(key);

            // Assert
            Assert.Equal(typeof(string), result);
        }

        [Fact]
        public void Transform_WithNoRules_ShouldReturnOriginalValue()
        {
            // Arrange
            var builder = new TransformBuilder();
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            var value = "test";

            // Act
            var result = manager.Transform(key, value);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void Transform_WithRules_ShouldApplyAllRulesInOrder()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>();
            var rule2 = new Mock<ITransformRule>();

            rule1.Setup(r => r.Transform(It.IsAny<object>())).Returns<object>(v => v?.ToString()?.ToUpper());
            rule2.Setup(r => r.Transform(It.IsAny<object>())).Returns<object>(v => v?.ToString() + "!");

            builder.AddRule("test", "1.1", rule1.Object, TransformDirection.Input);
            builder.AddRule("test", "1.2", rule2.Object, TransformDirection.Input);

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.Transform(key, "hello");

            // Assert
            Assert.Equal("HELLO!", result);
        }

        [Fact]
        public void Transform_WithStringParameters_ShouldWork()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>();
            rule.Setup(r => r.Transform(It.IsAny<object>())).Returns<object>(v => v?.ToString()?.ToUpper());
            builder.AddRule("test", "1.1", rule.Object, TransformDirection.Input);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);

            // Act
            var result = manager.Transform("test", "1.0", "hello", TransformDirection.Input);

            // Assert
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public async Task TransformAsync_WithNoRules_ShouldReturnOriginalValue()
        {
            // Arrange
            var builder = new TransformBuilder();
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            var value = "test";

            // Act
            var result = await manager.TransformAsync(key, value);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task TransformAsync_WithRules_ShouldApplyAllRulesInOrder()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>();
            var rule2 = new Mock<ITransformRule>();

            rule1.Setup(r => r.TransformAsync(It.IsAny<object>())).ReturnsAsync((object v) => v?.ToString()?.ToUpper());
            rule2.Setup(r => r.TransformAsync(It.IsAny<object>())).ReturnsAsync((object v) => v?.ToString() + "!");

            builder.AddRule("test", "1.1", rule1.Object, TransformDirection.Input);
            builder.AddRule("test", "1.2", rule2.Object, TransformDirection.Input);

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = await manager.TransformAsync(key, "hello");

            // Assert
            Assert.Equal("HELLO!", result);
        }

        [Fact]
        public async Task TransformAsync_WithStringParameters_ShouldWork()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>();
            rule.Setup(r => r.TransformAsync(It.IsAny<object>())).ReturnsAsync((object v) => v?.ToString()?.ToUpper());
            builder.AddRule("test", "1.1", rule.Object, TransformDirection.Input);
            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);

            // Act
            var result = await manager.TransformAsync("test", "1.0", "hello", TransformDirection.Input);

            // Assert
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void GetRequiredRules_ShouldOnlyReturnRulesWithHigherVersions()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;
            var rule3 = new Mock<ITransformRule>().Object;

            // Add rules with different versions
            builder.AddRule("test", "1.0", rule1, TransformDirection.Input); // Same version - should not be included
            builder.AddRule("test", "0.9", rule2, TransformDirection.Input); // Lower version - should not be included
            builder.AddRule("test", "1.1", rule3, TransformDirection.Input); // Higher version - should be included

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetRequiredRules(key);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(rule3, result[0]);
        }

        [Fact]
        public void GetRequiredRules_ShouldOnlyMatchSameKeyAndDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule1 = new Mock<ITransformRule>().Object;
            var rule2 = new Mock<ITransformRule>().Object;
            var rule3 = new Mock<ITransformRule>().Object;

            builder.AddRule("test", "1.1", rule1, TransformDirection.Input);
            builder.AddRule("other", "1.1", rule2, TransformDirection.Input); // Different key
            builder.AddRule("test", "1.1", rule3, TransformDirection.Output); // Different direction

            var serviceProvider = CreateServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = manager.GetRequiredRules(key);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(rule1, result[0]);
        }
    }
}