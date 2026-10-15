using BlackDigital.AspNet.Security;

namespace BlackDigital.AspNet.Test.Security
{
    public class Argon2idPasswordHashServiceTest
    {
        // Use low-cost parameters so tests run quickly.
        private static Argon2idPasswordHashService CreateService(
            int memorySize = 512,
            int iterations = 1,
            int parallelism = 1,
            int saltLength = 16,
            int hashLength = 32)
            => new(memorySize, iterations, parallelism, saltLength, hashLength);

        [Theory]
        [InlineData(7)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WhenMemorySizeBelowMinimum_ShouldThrow(int memorySize)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(memorySize: memorySize));
            Assert.Equal("memorySize", ex.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WhenIterationsBelowMinimum_ShouldThrow(int iterations)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(iterations: iterations));
            Assert.Equal("iterations", ex.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WhenParallelismBelowMinimum_ShouldThrow(int parallelism)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(parallelism: parallelism));
            Assert.Equal("parallelism", ex.ParamName);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WhenSaltLengthBelowMinimum_ShouldThrow(int saltLength)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(saltLength: saltLength));
            Assert.Equal("saltLength", ex.ParamName);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WhenHashLengthBelowMinimum_ShouldThrow(int hashLength)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(hashLength: hashLength));
            Assert.Equal("hashLength", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldNotThrow()
        {
            var ex = Record.Exception(() => CreateService());
            Assert.Null(ex);
        }

        [Fact]
        public void HashPassword_WhenPasswordIsValid_ShouldReturnPhcFormat()
        {
            var service = CreateService();

            var hash = service.HashPassword("myPassword123");

            Assert.StartsWith("$argon2id$v=19$", hash);
            var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(5, parts.Length);
            Assert.Equal("argon2id", parts[0]);
        }

        [Fact]
        public void HashPassword_ShouldEmbedParametersInHash()
        {
            var service = CreateService(memorySize: 1024, iterations: 2, parallelism: 1);

            var hash = service.HashPassword("password");

            Assert.Contains("m=1024,t=2,p=1", hash);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HashPassword_WhenPasswordIsNullOrEmpty_ShouldThrow(string? password)
        {
            var service = CreateService();

            var ex = Assert.Throws<ArgumentException>(() => service.HashPassword(password!));
            Assert.Equal("password", ex.ParamName);
        }

        [Fact]
        public void HashPassword_ShouldProduceDifferentHashesForSamePassword()
        {
            var service = CreateService();

            var hash1 = service.HashPassword("samePassword");
            var hash2 = service.HashPassword("samePassword");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            var service = CreateService();
            var hash = service.HashPassword("correctPassword");

            var result = service.VerifyPassword("correctPassword", hash);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            var service = CreateService();
            var hash = service.HashPassword("correctPassword");

            var result = service.VerifyPassword("wrongPassword", hash);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void VerifyPassword_WhenPasswordIsNullOrEmpty_ShouldReturnFalse(string? password)
        {
            var service = CreateService();
            var hash = service.HashPassword("valid");

            var result = service.VerifyPassword(password!, hash);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void VerifyPassword_WhenHashIsNullOrEmpty_ShouldReturnFalse(string? hash)
        {
            var service = CreateService();

            var result = service.VerifyPassword("valid", hash!);

            Assert.False(result);
        }

        [Theory]
        [InlineData("not-a-hash")]
        [InlineData("$argon2id$")]
        [InlineData("$argon2id$v=19$m=512,t=1,p=1$invalidBase64!!!$invalid")]
        [InlineData("$notargon2$v=19$m=512,t=1,p=1$c2FsdA==$aGFzaA==")]
        public void VerifyPassword_WithMalformedHash_ShouldReturnFalse(string hash)
        {
            var service = CreateService();

            var result = service.VerifyPassword("password", hash);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_WithDifferentParameters_ShouldStillVerify()
        {
            // Hash with one set of params, verify with a service using different
            // default params — the hash embeds the params, so it should work.
            var hashService = CreateService(memorySize: 1024, iterations: 1, parallelism: 1);
            var verifyService = CreateService(memorySize: 512, iterations: 2, parallelism: 1);

            var hash = hashService.HashPassword("password");
            var result = verifyService.VerifyPassword("password", hash);

            Assert.True(result);
        }
    }
}
