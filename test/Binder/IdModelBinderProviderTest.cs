using BlackDigital.Model;
using BlackDigital.Mvc.Binder;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace BlackDigital.Mvc.Test.Binder
{
    public class IdModelBinderProviderTest
    {
        private readonly IdModelBinderProvider _provider;

        public IdModelBinderProviderTest()
        {
            _provider = new IdModelBinderProvider();
        }

        [Fact]
        public void GetBinder_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => _provider.GetBinder(null!));
            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void GetBinder_WhenModelTypeIsId_ShouldReturnIdBinder()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(Id));
            var context = new TestModelBinderProviderContext(metadata);

            // Act
            var result = _provider.GetBinder(context);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<IdBinder>(result);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(object))]
        public void GetBinder_WhenModelTypeIsNotId_ShouldReturnNull(Type modelType)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(modelType);
            var context = new TestModelBinderProviderContext(metadata);

            // Act
            var result = _provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBinder_ShouldReturnNewInstanceEachTime()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(Id));
            var context = new TestModelBinderProviderContext(metadata);

            // Act
            var result1 = _provider.GetBinder(context);
            var result2 = _provider.GetBinder(context);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotSame(result1, result2);
        }

        private class TestModelBinderProviderContext : ModelBinderProviderContext
        {
            public TestModelBinderProviderContext(ModelMetadata metadata)
            {
                Metadata = metadata;
                BindingInfo = new BindingInfo();
                MetadataProvider = new EmptyModelMetadataProvider();
            }

            public override ModelMetadata Metadata { get; }
            public override BindingInfo BindingInfo { get; }
            public override IModelMetadataProvider MetadataProvider { get; }

            public override IModelBinder CreateBinder(ModelMetadata metadata)
            {
                throw new NotImplementedException();
            }
        }
    }
}