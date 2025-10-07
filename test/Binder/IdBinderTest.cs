using BlackDigital.Model;
using BlackDigital.Mvc.Binder;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace BlackDigital.Mvc.Test.Binder
{
    public class IdBinderTest
    {
        private readonly IdBinder _idBinder;
        private readonly Mock<ModelBindingContext> _mockBindingContext;
        private readonly Mock<IValueProvider> _mockValueProvider;
        private readonly ModelStateDictionary _modelState;

        public IdBinderTest()
        {
            _idBinder = new IdBinder();
            _mockBindingContext = new Mock<ModelBindingContext>();
            _mockValueProvider = new Mock<IValueProvider>();
            _modelState = new ModelStateDictionary();

            // Setup básico do contexto
            _mockBindingContext.Setup(x => x.ValueProvider).Returns(_mockValueProvider.Object);
            _mockBindingContext.Setup(x => x.ModelState).Returns(_modelState);
            _mockBindingContext.SetupProperty(x => x.Result);
            _mockBindingContext.Setup(x => x.FieldName).Returns("testField");
            _mockBindingContext.Setup(x => x.ModelName).Returns("testModel");
        }

        [Fact]
        public async Task BindModelAsync_WhenValueProviderResultIsNone_ShouldReturnCompletedTask()
        {
            // Arrange
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(ValueProviderResult.None);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Empty(_modelState);
            Assert.Equal(ModelBindingResult.Failed(), _mockBindingContext.Object.Result);
        }

        [Fact]
        public async Task BindModelAsync_WhenValueIsNull_ShouldNotSetModelState()
        {
            // Arrange
            // Para valores null, vamos usar ValueProviderResult.None
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(ValueProviderResult.None);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Empty(_modelState);
            Assert.Equal(ModelBindingResult.Failed(), _mockBindingContext.Object.Result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BindModelAsync_WhenValueIsEmptyOrWhiteSpace_ShouldSetModelStateButNotResult(string value)
        {
            // Arrange
            var valueProviderResult = new ValueProviderResult(value);
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(valueProviderResult);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Single(_modelState);
            Assert.True(_modelState.ContainsKey("testModel"));
            Assert.Equal(ModelBindingResult.Failed(), _mockBindingContext.Object.Result);
        }

        [Fact]
        public async Task BindModelAsync_WhenValueIsValid_ShouldCreateIdAndSetSuccessResult()
        {
            // Arrange
            const string validIdValue = "123e4567-e89b-12d3-a456-426614174000";
            var valueProviderResult = new ValueProviderResult(validIdValue);
            
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(valueProviderResult);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Single(_modelState);
            Assert.True(_modelState.ContainsKey("testModel"));
            
            Assert.True(_mockBindingContext.Object.Result.IsModelSet);
            Assert.IsType<Id>(_mockBindingContext.Object.Result.Model);
            
            var resultId = (Id)_mockBindingContext.Object.Result.Model;
            Assert.Equal(validIdValue, resultId.ToString());
        }

        [Fact]
        public async Task BindModelAsync_WhenValueIsValidGuid_ShouldCreateIdSuccessfully()
        {
            // Arrange
            var guidValue = Guid.NewGuid().ToString();
            var valueProviderResult = new ValueProviderResult(guidValue);
            
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(valueProviderResult);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Single(_modelState);
            Assert.True(_modelState.ContainsKey("testModel"));
            
            Assert.True(_mockBindingContext.Object.Result.IsModelSet);
            Assert.IsType<Id>(_mockBindingContext.Object.Result.Model);
        }

        [Fact]
        public async Task BindModelAsync_WhenValueIsSimpleString_ShouldCreateIdSuccessfully()
        {
            // Arrange
            const string simpleValue = "test-id-123";
            var valueProviderResult = new ValueProviderResult(simpleValue);
            
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(valueProviderResult);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Single(_modelState);
            Assert.True(_modelState.ContainsKey("testModel"));
            
            Assert.True(_mockBindingContext.Object.Result.IsModelSet);
            Assert.IsType<Id>(_mockBindingContext.Object.Result.Model);
            
            var resultId = (Id)_mockBindingContext.Object.Result.Model;
            Assert.Equal(simpleValue, resultId.ToString());
        }

        [Fact]
        public async Task BindModelAsync_ShouldAlwaysSetModelStateValue_WhenValueProviderResultIsNotNone()
        {
            // Arrange
            const string testValue = "any-value";
            var valueProviderResult = new ValueProviderResult(testValue);
            
            _mockValueProvider
                .Setup(x => x.GetValue("testField"))
                .Returns(valueProviderResult);

            // Act
            await _idBinder.BindModelAsync(_mockBindingContext.Object);

            // Assert
            Assert.Single(_modelState);
            Assert.True(_modelState.ContainsKey("testModel"));
        }
    }
}