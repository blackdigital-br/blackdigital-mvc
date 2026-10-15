using BlackDigital.AspNet.Rest;
using BlackDigital.Rest;
using System.Threading;
using Xunit;

namespace BlackDigital.AspNet.Test.Rest
{
    public interface IParameterMetadataTestService
    {
        Task<string> GetAsync([Path] int id, [Query] string name, CancellationToken ct);

        Task CreateAsync([Body] object data);
    }

    public class ParameterMetadataCacheTest
    {
        [Fact]
        public void Get_ShouldReturnMetadataForAllParameters()
        {
            // Arrange
            var method = typeof(IParameterMetadataTestService).GetMethod(nameof(IParameterMetadataTestService.GetAsync))!;

            // Act
            var metadata = ParameterMetadataCache.Get(method);

            // Assert
            Assert.Equal(3, metadata.Length);
            Assert.Equal(typeof(int), metadata[0].ParameterType);
            Assert.NotNull(metadata[0].RouteAttribute);
            Assert.Equal("id", metadata[0].RouteKey);
            Assert.True(metadata[0].IsSimpleType);

            Assert.Equal(typeof(string), metadata[1].ParameterType);
            Assert.NotNull(metadata[1].QueryAttribute);
            Assert.Equal("name", metadata[1].QueryKey);

            Assert.Equal(typeof(CancellationToken), metadata[2].ParameterType);
            Assert.True(metadata[2].IsCancellationToken);
        }

        [Fact]
        public void Get_ShouldReturnBodyMetadata()
        {
            // Arrange
            var method = typeof(IParameterMetadataTestService).GetMethod(nameof(IParameterMetadataTestService.CreateAsync))!;

            // Act
            var metadata = ParameterMetadataCache.Get(method);

            // Assert
            Assert.Single(metadata);
            Assert.True(metadata[0].IsBody);
            Assert.NotNull(metadata[0].BodyAttribute);
        }

        [Fact]
        public void Get_WhenCalledMultipleTimes_ShouldReturnCachedInstance()
        {
            // Arrange
            var method = typeof(IParameterMetadataTestService).GetMethod(nameof(IParameterMetadataTestService.GetAsync))!;

            // Act
            var first = ParameterMetadataCache.Get(method);
            var second = ParameterMetadataCache.Get(method);

            // Assert
            Assert.Same(first, second);
        }
    }
}
