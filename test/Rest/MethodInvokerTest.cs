using BlackDigital.AspNet.Rest;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BlackDigital.AspNet.Test.Rest
{
    public class MethodInvokerTest
    {
        private class Calculator
        {
            public int Add(int a, int b) => a + b;
            public string Greet(string name) => $"Hello, {name}!";
            public void VoidMethod() { }
            public int GetValue() => 42;
            public Task<int> GetAsyncValue() => Task.FromResult(99);
            public ValueTask<string> GetValueTaskResult() => new ValueTask<string>("vt-result");
        }

        [Fact]
        public void Invoke_WhenMethodHasTwoParameters_ShouldReturnCorrectResult()
        {
            // Arrange
            var method = typeof(Calculator).GetMethod(nameof(Calculator.Add))!;
            var calculator = new Calculator();

            // Act
            var result = MethodInvoker.Invoke(method, calculator, new object?[] { 3, 4 });

            // Assert
            Assert.Equal(7, result);
        }

        [Fact]
        public void Invoke_WhenMethodReturnsString_ShouldReturnString()
        {
            // Arrange
            var method = typeof(Calculator).GetMethod(nameof(Calculator.Greet))!;
            var calculator = new Calculator();

            // Act
            var result = MethodInvoker.Invoke(method, calculator, new object?[] { "World" });

            // Assert
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void Invoke_WhenMethodIsVoid_ShouldReturnNull()
        {
            // Arrange
            var method = typeof(Calculator).GetMethod(nameof(Calculator.VoidMethod))!;
            var calculator = new Calculator();

            // Act
            var result = MethodInvoker.Invoke(method, calculator, System.Array.Empty<object?>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Invoke_WhenMethodReturnsValue_ShouldReturnValue()
        {
            // Arrange
            var method = typeof(Calculator).GetMethod(nameof(Calculator.GetValue))!;
            var calculator = new Calculator();

            // Act
            var result = MethodInvoker.Invoke(method, calculator, System.Array.Empty<object?>());

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Invoke_WhenMethodIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var calculator = new Calculator();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MethodInvoker.Invoke(null!, calculator, System.Array.Empty<object?>()));
        }

        [Fact]
        public void Invoke_WhenTargetIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var method = typeof(Calculator).GetMethod(nameof(Calculator.Add))!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MethodInvoker.Invoke(method, null!, new object?[] { 1, 2 }));
        }

        [Fact]
        public async Task GetTaskResult_WhenTaskIsGeneric_ShouldReturnValue()
        {
            // Arrange
            var task = Task.FromResult(123);
            var taskType = typeof(Task<int>);

            // Act
            var result = MethodInvoker.GetTaskResult(task, taskType);

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public void GetTaskResult_WhenTaskIsNotGeneric_ShouldReturnNull()
        {
            // Arrange
            var task = Task.CompletedTask;
            var taskType = typeof(Task);

            // Act
            var result = MethodInvoker.GetTaskResult(task, taskType);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetValueTaskResult_WhenValueTaskIsGeneric_ShouldReturnValue()
        {
            // Arrange
            var valueTask = new ValueTask<string>("hello");
            var valueTaskType = typeof(ValueTask<string>);

            // Act
            var result = MethodInvoker.GetValueTaskResult(valueTask, valueTaskType);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void GetValueTaskResult_WhenValueTaskIsNotGeneric_ShouldReturnNull()
        {
            // Arrange
            var valueTask = new ValueTask();
            var valueTaskType = typeof(ValueTask);

            // Act
            var result = MethodInvoker.GetValueTaskResult(valueTask, valueTaskType);

            // Assert
            Assert.Null(result);
        }
    }
}
