using BlackDigital.AspNet.Rest;
using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.AspNet.Test.Rest.Transforms;
using BlackDigital.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace BlackDigital.AspNet.Test.Rest
{
    public class TransformAttributeTest
    {
        [Service("api/transform-products")]
        public interface IProductTransformService
        {
            [Action("", method: RestMethod.Get)]
            [Transform(typeof(TestTransformRule), "v2", TransformDirection.Output)]
            Task<string> GetAllAsync();

            [Action("", method: RestMethod.Post)]
            [Transform(typeof(TestTransformRule), "v1", TransformDirection.Input)]
            Task CreateAsync([Body] string data);
        }

        [Fact]
        public void Constructor_WithValidRuleType_ShouldSetProperties()
        {
            // Arrange & Act
            var attr = new TransformAttribute(typeof(TestTransformRule), "v1", TransformDirection.Output);

            // Assert
            Assert.Equal(typeof(TestTransformRule), attr.RuleType);
            Assert.Equal("v1", attr.Version);
            Assert.Equal(TransformDirection.Output, attr.Direction);
        }

        [Fact]
        public void Constructor_WithDefaultDirection_ShouldDefaultToInput()
        {
            // Arrange & Act
            var attr = new TransformAttribute(typeof(TestTransformRule), "v1");

            // Assert
            Assert.Equal(TransformDirection.Input, attr.Direction);
        }

        [Fact]
        public void Constructor_WithNullRuleType_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TransformAttribute(null!, "v1"));
        }

        [Fact]
        public void Constructor_WithNonTransformRuleType_ShouldThrowArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TransformAttribute(typeof(string), "v1"));
            Assert.Contains("does not implement ITransformRule", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithNullOrWhitespaceVersion_ShouldThrowArgumentException(string? version)
        {
            Assert.Throws<ArgumentException>(() => new TransformAttribute(typeof(TestTransformRule), version!));
        }

        [Fact]
        public void AddRestServices_WhenActionHasTransformAttribute_ShouldRegisterRuleInTransformBuilder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRestServices(rest =>
                rest.AddService<IProductTransformService, ProductTransformServiceImpl>());

            // Assert
            var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<TransformManager>();

            var outputKey = new TransformKey("GET:api/transform-products/", "v2", TransformDirection.Output);
            Assert.True(manager.HasRule(outputKey));

            var inputKey = new TransformKey("POST:api/transform-products/", "v1", TransformDirection.Input);
            Assert.True(manager.HasRule(inputKey));
        }

        [Fact]
        public void AddRestServices_WhenActionHasTransformAttribute_ShouldApplyTransform()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRestServices(rest =>
                rest.AddService<IProductTransformService, ProductTransformServiceImpl>());

            var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<TransformManager>();

            // Act
            var result = manager.Transform(new TransformKey("GET:api/transform-products/", "v1", TransformDirection.Output), "hello");

            // Assert
            // v1 < v2, so the v2 output rule is applied (uppercases the string).
            Assert.Equal("HELLO", result);
        }

        public class ProductTransformServiceImpl : IProductTransformService
        {
            public Task<string> GetAllAsync() => Task.FromResult("test");
            public Task CreateAsync(string data) => Task.CompletedTask;
        }
    }
}
