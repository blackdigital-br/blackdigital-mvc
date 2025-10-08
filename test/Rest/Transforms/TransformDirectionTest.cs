using BlackDigital.Mvc.Rest.Trasnforms;

namespace BlackDigital.Mvc.Test.Rest.Transforms
{
    public class TransformDirectionTest
    {
        [Fact]
        public void TransformDirection_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal(1, (int)TransformDirection.Input);
            Assert.Equal(2, (int)TransformDirection.Output);
            Assert.Equal(3, (int)TransformDirection.Both);
        }

        [Fact]
        public void TransformDirection_Both_ShouldBeCombinationOfInputAndOutput()
        {
            // Assert
            Assert.Equal(TransformDirection.Input | TransformDirection.Output, TransformDirection.Both);
        }

        [Fact]
        public void TransformDirection_ShouldSupportFlagsOperations()
        {
            // Arrange & Act
            var combined = TransformDirection.Input | TransformDirection.Output;

            // Assert
            Assert.Equal(TransformDirection.Both, combined);
            Assert.True((TransformDirection.Both & TransformDirection.Input) == TransformDirection.Input);
            Assert.True((TransformDirection.Both & TransformDirection.Output) == TransformDirection.Output);
        }
    }
}