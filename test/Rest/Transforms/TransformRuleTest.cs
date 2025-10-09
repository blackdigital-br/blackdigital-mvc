using BlackDigital.AspNet.Rest.Trasnforms;

namespace BlackDigital.AspNet.Test.Rest.Transforms
{
    // Concrete test classes for testing abstract TransformRule classes
    public class TestTransformRule : TransformRule
    {
        public override Type? InputType => typeof(string);
        public override Type? OutputType => typeof(string);

        public override object? Transform(object? value)
        {
            return value?.ToString()?.ToUpper();
        }
    }

    public class TestTransformRuleGeneric : TransformRule<string>
    {
        public override string? Transform(string? value)
        {
            return value?.ToLower();
        }
    }

    public class TestTransformRuleGenericInOut : TransformRule<string, int>
    {
        public override int Transform(string? value)
        {
            return value?.Length ?? 0;
        }
    }

    public class AsyncTestTransformRule : TransformRule
    {
        public override async Task<object?> TransformAsync(object? value)
        {
            await Task.Delay(10);
            return value?.ToString()?.ToUpper();
        }
    }

    public class TransformRuleTest
    {
        [Fact]
        public void TransformRule_DefaultImplementation_ShouldReturnNullTypes()
        {
            // Arrange
            var rule = new TestTransformRule();

            // Act & Assert
            Assert.Equal(typeof(string), rule.InputType);
            Assert.Equal(typeof(string), rule.OutputType);
        }

        [Fact]
        public void TransformRule_Transform_ShouldTransformValue()
        {
            // Arrange
            var rule = new TestTransformRule();
            var input = "hello";

            // Act
            var result = rule.Transform(input);

            // Assert
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void TransformRule_Transform_WithNull_ShouldHandleNull()
        {
            // Arrange
            var rule = new TestTransformRule();

            // Act
            var result = rule.Transform(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TransformRule_TransformAsync_ShouldCallTransform()
        {
            // Arrange
            var rule = new TestTransformRule();
            var input = "hello";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public async Task TransformRule_TransformAsync_WithCustomImplementation_ShouldWork()
        {
            // Arrange
            var rule = new AsyncTestTransformRule();
            var input = "hello";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void TransformRuleGeneric_ShouldHaveCorrectTypes()
        {
            // Arrange
            var rule = new TestTransformRuleGeneric();

            // Act & Assert
            Assert.Equal(typeof(string), rule.InputType);
            Assert.Equal(typeof(string), rule.OutputType);
        }

        [Fact]
        public void TransformRuleGeneric_Transform_ShouldTransformValue()
        {
            // Arrange
            var rule = new TestTransformRuleGeneric();
            var input = "HELLO";

            // Act
            var result = rule.Transform(input);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void TransformRuleGeneric_ITransformRule_Transform_ShouldWork()
        {
            // Arrange
            ITransformRule rule = new TestTransformRuleGeneric();
            var input = "HELLO";

            // Act
            var result = rule.Transform(input);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task TransformRuleGeneric_ITransformRule_TransformAsync_ShouldWork()
        {
            // Arrange
            ITransformRule rule = new TestTransformRuleGeneric();
            var input = "HELLO";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void TransformRuleGenericInOut_ShouldHaveCorrectTypes()
        {
            // Arrange
            var rule = new TestTransformRuleGenericInOut();

            // Act & Assert
            Assert.Equal(typeof(string), rule.InputType);
            Assert.Equal(typeof(int), rule.OutputType);
        }

        [Fact]
        public void TransformRuleGenericInOut_Transform_ShouldTransformValue()
        {
            // Arrange
            var rule = new TestTransformRuleGenericInOut();
            var input = "hello";

            // Act
            var result = rule.Transform(input);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void TransformRuleGenericInOut_Transform_WithNull_ShouldReturnZero()
        {
            // Arrange
            var rule = new TestTransformRuleGenericInOut();

            // Act
            var result = rule.Transform(null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void TransformRuleGenericInOut_ITransformRule_Transform_ShouldWork()
        {
            // Arrange
            ITransformRule rule = new TestTransformRuleGenericInOut();
            var input = "hello";

            // Act
            var result = rule.Transform(input);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task TransformRuleGenericInOut_ITransformRule_TransformAsync_ShouldWork()
        {
            // Arrange
            ITransformRule rule = new TestTransformRuleGenericInOut();
            var input = "hello";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task TransformRuleGeneric_TransformAsync_ShouldWork()
        {
            // Arrange
            var rule = new TestTransformRuleGeneric();
            var input = "HELLO";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task TransformRuleGenericInOut_TransformAsync_ShouldWork()
        {
            // Arrange
            var rule = new TestTransformRuleGenericInOut();
            var input = "hello";

            // Act
            var result = await rule.TransformAsync(input);

            // Assert
            Assert.Equal(5, result);
        }
    }
}
