using BlackDigital.Mvc.Rest.Trasnforms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlackDigital.Mvc.Test.Rest.Transforms
{
    public class TransformBuilderTest
    {
        [Fact]
        public void AddRule_WithKeyAndRule_ShouldAddRuleToBuilder()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddRule(key, rule);

            // Assert
            Assert.Same(builder, result); // Fluent interface
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void AddRule_WithStringParameters_ShouldAddRuleWithDefaultDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddRule("test", "1.0", rule);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var expectedKey = new TransformKey("test", "1.0", TransformDirection.Input);
            Assert.True(manager.HasRule(expectedKey));
        }

        [Fact]
        public void AddRule_WithStringParametersAndDirection_ShouldAddRuleWithSpecifiedDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddRule("test", "1.0", rule, TransformDirection.Output);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var expectedKey = new TransformKey("test", "1.0", TransformDirection.Output);
            Assert.True(manager.HasRule(expectedKey));
        }

        [Fact]
        public void AddInputRule_ShouldAddRuleWithInputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddInputRule("test", "1.0", rule);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var expectedKey = new TransformKey("test", "1.0", TransformDirection.Input);
            Assert.True(manager.HasRule(expectedKey));
        }

        [Fact]
        public void AddOutputRule_ShouldAddRuleWithOutputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddOutputRule("test", "1.0", rule);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var expectedKey = new TransformKey("test", "1.0", TransformDirection.Output);
            Assert.True(manager.HasRule(expectedKey));
        }

        [Fact]
        public void AddInputAndOutputRule_ShouldAddRuleWithBothDirection()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act
            var result = builder.AddInputAndOutputRule("test", "1.0", rule);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            var expectedKey = new TransformKey("test", "1.0", TransformDirection.Both);
            Assert.True(manager.HasRule(expectedKey));
        }

        [Fact]
        public void AddRule_WithMultipleKeys_ShouldAddRuleForAllKeys()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Output);

            // Act
            var result = builder.AddRule(rule, key1, key2);

            // Assert
            Assert.Same(builder, result);
            // Verify the rules were added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key1));
            Assert.True(manager.HasRule(key2));
        }

        [Fact]
        public void AddRule_WithMultipleKeys_EmptyArray_ShouldThrowException()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.AddRule(rule));
            Assert.Contains("At least one key must be provided", exception.Message);
        }

        [Fact]
        public void AddRule_WithType_ShouldAddRuleType()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = builder.AddRule(key, typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void AddRule_WithInvalidType_ShouldThrowException()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.AddRule(key, typeof(string)));
            Assert.Contains("does not implement ITransformRule", exception.Message);
        }

        [Fact]
        public void AddRule_WithTypeAndStringParameters_ShouldAddRuleTypeWithDefaultDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddRule("test", "1.0", typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Input));
        }

        [Fact]
        public void AddInputRule_WithType_ShouldAddRuleTypeWithInputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddInputRule("test", "1.0", typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Input));
        }

        [Fact]
        public void AddOutputRule_WithType_ShouldAddRuleTypeWithOutputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddOutputRule("test", "1.0", typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Output));
        }

        [Fact]
        public void AddInputAndOutputRule_WithType_ShouldAddRuleTypeWithBothDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddInputAndOutputRule("test", "1.0", typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Both));
        }

        [Fact]
        public void AddRule_WithTypeAndMultipleKeys_ShouldAddRuleTypeForAllKeys()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Output);

            // Act
            var result = builder.AddRule(typeof(TestTransformRule), key1, key2);

            // Assert
            Assert.Same(builder, result);
            // Verify the rules were added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key1));
            Assert.True(manager.HasRule(key2));
        }

        [Fact]
        public void AddRule_WithTypeAndMultipleKeys_EmptyArray_ShouldThrowException()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.AddRule(typeof(TestTransformRule)));
            Assert.Contains("At least one key must be provided", exception.Message);
        }

        [Fact]
        public void AddRule_WithGenericType_ShouldAddRuleType()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = builder.AddRule<TestTransformRule>(key);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void AddRule_WithGenericTypeAndStringParameters_ShouldAddRuleTypeWithDefaultDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddRule<TestTransformRule>("test", "1.0");

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Input));
        }

        [Fact]
        public void AddInputRule_WithGenericType_ShouldAddRuleTypeWithInputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddInputRule<TestTransformRule>("test", "1.0");

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Input));
        }

        [Fact]
        public void AddOutputRule_WithGenericType_ShouldAddRuleTypeWithOutputDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddOutputRule<TestTransformRule>("test", "1.0");

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Output));
        }

        [Fact]
        public void AddInputAndOutputRule_WithGenericType_ShouldAddRuleTypeWithBothDirection()
        {
            // Arrange
            var builder = new TransformBuilder();

            // Act
            var result = builder.AddInputAndOutputRule<TestTransformRule>("test", "1.0");

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule("test", "1.0", TransformDirection.Both));
        }

        [Fact]
        public void AddRule_WithGenericTypeAndMultipleKeys_ShouldAddRuleTypeForAllKeys()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Output);

            // Act
            var result = builder.AddRule<TestTransformRule>(key1, key2);

            // Assert
            Assert.Same(builder, result);
            // Verify the rules were added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key1));
            Assert.True(manager.HasRule(key2));
        }

        [Fact]
        public void AddRule_WithRuleInstance_ShouldWorkCorrectly()
        {
            // Arrange
            var builder = new TransformBuilder();
            var rule = new Mock<ITransformRule>().Object;
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = builder.AddRule(key, rule);

            // Assert
            Assert.Same(builder, result);
            // Verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key));
        }

        [Fact]
        public void AddRule_WithTypeBasedRule_ShouldWorkCorrectly()
        {
            // Arrange
            var builder = new TransformBuilder();
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var result = builder.AddRule(key, typeof(TestTransformRule));

            // Assert
            Assert.Same(builder, result);
            // We can't directly test GetRulesType since it's internal,
            // but we can verify the rule was added by creating a manager
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var manager = new TransformManager(builder, serviceProvider);
            Assert.True(manager.HasRule(key));
        }
    }
}