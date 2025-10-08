using BlackDigital.Mvc.Rest.Trasnforms;

namespace BlackDigital.Mvc.Test.Rest.Transforms
{
    public class TransformDirectionHelperTest
    {
        [Fact]
        public void HasInput_WithInputDirection_ShouldReturnTrue()
        {
            // Arrange
            var direction = TransformDirection.Input;

            // Act
            var result = direction.HasInput();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasInput_WithOutputDirection_ShouldReturnFalse()
        {
            // Arrange
            var direction = TransformDirection.Output;

            // Act
            var result = direction.HasInput();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasInput_WithBothDirection_ShouldReturnTrue()
        {
            // Arrange
            var direction = TransformDirection.Both;

            // Act
            var result = direction.HasInput();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasOutput_WithOutputDirection_ShouldReturnTrue()
        {
            // Arrange
            var direction = TransformDirection.Output;

            // Act
            var result = direction.HasOutput();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasOutput_WithInputDirection_ShouldReturnFalse()
        {
            // Arrange
            var direction = TransformDirection.Input;

            // Act
            var result = direction.HasOutput();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasOutput_WithBothDirection_ShouldReturnTrue()
        {
            // Arrange
            var direction = TransformDirection.Both;

            // Act
            var result = direction.HasOutput();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Enumerate_WithInputDirection_ShouldReturnOnlyInput()
        {
            // Arrange
            var direction = TransformDirection.Input;

            // Act
            var result = direction.Enumerate().ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains(TransformDirection.Input, result);
        }

        [Fact]
        public void Enumerate_WithOutputDirection_ShouldReturnOnlyOutput()
        {
            // Arrange
            var direction = TransformDirection.Output;

            // Act
            var result = direction.Enumerate().ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains(TransformDirection.Output, result);
        }

        [Fact]
        public void Enumerate_WithBothDirection_ShouldReturnInputAndOutput()
        {
            // Arrange
            var direction = TransformDirection.Both;

            // Act
            var result = direction.Enumerate().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(TransformDirection.Input, result);
            Assert.Contains(TransformDirection.Output, result);
        }
    }
}