using BlackDigital.AspNet.Rest.Trasnforms;

namespace BlackDigital.AspNet.Test.Rest.Transforms
{
    public class TransformKeyTest
    {
        [Fact]
        public void Constructor_WithAllParameters_ShouldSetProperties()
        {
            // Arrange
            var key = "testKey";
            var version = "1.0";
            var direction = TransformDirection.Output;

            // Act
            var transformKey = new TransformKey(key, version, direction);

            // Assert
            Assert.Equal(key, transformKey.Key);
            Assert.Equal(version, transformKey.Version);
            Assert.Equal(direction, transformKey.Direction);
        }

        [Fact]
        public void Constructor_WithKeyAndVersion_ShouldDefaultToInputDirection()
        {
            // Arrange
            var key = "testKey";
            var version = "1.0";

            // Act
            var transformKey = new TransformKey(key, version);

            // Assert
            Assert.Equal(key, transformKey.Key);
            Assert.Equal(version, transformKey.Version);
            Assert.Equal(TransformDirection.Input, transformKey.Direction);
        }

        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.True(key1.Equals(key2));
            Assert.True(key1.Equals((object)key2));
        }

        [Fact]
        public void Equals_WithDifferentKey_ShouldReturnFalse()
        {
            // Arrange
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key1.Equals(key2));
            Assert.False(key1.Equals((object)key2));
        }

        [Fact]
        public void Equals_WithDifferentVersion_ShouldReturnFalse()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "2.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key1.Equals(key2));
            Assert.False(key1.Equals((object)key2));
        }

        [Fact]
        public void Equals_WithDifferentDirection_ShouldReturnFalse()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "1.0", TransformDirection.Output);

            // Act & Assert
            Assert.False(key1.Equals(key2));
            Assert.False(key1.Equals((object)key2));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var key = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key.Equals("string"));
        }

        [Fact]
        public void GetHashCode_WithSameValues_ShouldReturnSameHash()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act
            var hash1 = key1.GetHashCode();
            var hash2 = key2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHash()
        {
            // Arrange
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Input);

            // Act
            var hash1 = key1.GetHashCode();
            var hash2 = key2.GetHashCode();

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void EqualityOperator_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.True(key1 == key2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key1 == key2);
        }

        [Fact]
        public void InequalityOperator_WithSameValues_ShouldReturnFalse()
        {
            // Arrange
            var key1 = new TransformKey("test", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.False(key1 != key2);
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ShouldReturnTrue()
        {
            // Arrange
            var key1 = new TransformKey("test1", "1.0", TransformDirection.Input);
            var key2 = new TransformKey("test2", "1.0", TransformDirection.Input);

            // Act & Assert
            Assert.True(key1 != key2);
        }

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var key = new TransformKey("testKey", "1.0", TransformDirection.Output);

            // Act
            var result = key.ToString();

            // Assert
            Assert.Equal("testKey:1.0:Output", result);
        }

        [Theory]
        [InlineData("", "1.0", TransformDirection.Input)]
        [InlineData("test", "", TransformDirection.Input)]
        [InlineData("test", "1.0", TransformDirection.Both)]
        public void Constructor_WithVariousInputs_ShouldWorkCorrectly(string keyValue, string version, TransformDirection direction)
        {
            // Act
            var transformKey = new TransformKey(keyValue, version, direction);

            // Assert
            Assert.Equal(keyValue, transformKey.Key);
            Assert.Equal(version, transformKey.Version);
            Assert.Equal(direction, transformKey.Direction);
        }
    }
}
