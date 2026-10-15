using System.Collections.Concurrent;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace BlackDigital.AspNet.Rest
{
    /// <summary>
    /// Compiles and caches strongly-typed delegates for invoking service methods
    /// and extracting results from asynchronous return types, replacing the
    /// slower reflection calls (<c>MethodInfo.Invoke</c>,
    /// <c>PropertyInfo.GetValue</c>, <c>MethodInfo.MakeGenericMethod</c>) with
    /// compiled expression trees that run nearly as fast as direct calls.
    /// </summary>
    /// <remarks>
    /// Each <see cref="MethodInfo"/> and each async result type is compiled once
    /// and cached for the lifetime of the application.
    /// </remarks>
    public static class MethodInvoker
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object?[], object?>> _invokers = new();
        private static readonly ConcurrentDictionary<Type, Func<Task, object?>> _taskResultExtractors = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object?>> _valueTaskResultExtractors = new();
        private static readonly ConcurrentDictionary<Type, Func<Stream, object, JsonSerializerOptions, Task>> _asyncEnumerableSerializers = new();

        /// <summary>
        /// Invokes the specified method on the target instance with the provided
        /// arguments using a cached compiled delegate.
        /// </summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="target">The instance on which to invoke the method.</param>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <returns>The method's return value, or null for void methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> or <paramref name="target"/> is null.</exception>
        public static object? Invoke(MethodInfo method, object target, object?[] arguments)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var invoker = _invokers.GetOrAdd(method, Compile);
            return invoker(target, arguments);
        }

        /// <summary>
        /// Extracts the <c>Result</c> property value from an awaited
        /// <see cref="Task{TResult}"/> instance using a cached compiled
        /// expression, avoiding per-call reflection.
        /// </summary>
        /// <param name="task">The awaited task whose result to extract.</param>
        /// <param name="taskType">The concrete generic <c>Task{TResult}</c> type.</param>
        /// <returns>The task's result value, or null when the type is not a generic task.</returns>
        public static object? GetTaskResult(Task task, Type taskType)
        {
            if (!taskType.IsGenericType)
                return null;

            var extractor = _taskResultExtractors.GetOrAdd(taskType, CompileTaskResultExtractor);
            return extractor(task);
        }

        /// <summary>
        /// Extracts the <c>Result</c> property value from a
        /// <see cref="ValueTask{TResult}"/> instance using a cached compiled
        /// expression, avoiding per-call reflection.
        /// </summary>
        /// <param name="valueTask">The value task (already awaited) whose result to extract.</param>
        /// <param name="valueTaskType">The concrete generic <c>ValueTask{TResult}</c> type.</param>
        /// <returns>The value task's result value, or null when the type is not a generic value task.</returns>
        public static object? GetValueTaskResult(object valueTask, Type valueTaskType)
        {
            if (!valueTaskType.IsGenericType)
                return null;

            var extractor = _valueTaskResultExtractors.GetOrAdd(valueTaskType, CompileValueTaskResultExtractor);
            return extractor(valueTask);
        }

        /// <summary>
        /// Serializes an <see cref="IAsyncEnumerable{T}"/> to a stream using
        /// <see cref="JsonSerializer.SerializeAsync{T}(Stream, T, JsonSerializerOptions, CancellationToken)"/>,
        /// caching the generic method call per element type to avoid repeated
        /// reflection-based <c>MakeGenericMethod</c> invocations.
        /// </summary>
        /// <param name="stream">The stream to write the JSON to.</param>
        /// <param name="asyncEnumerable">The async enumerable to serialize.</param>
        /// <param name="elementType">The element type of the async enumerable.</param>
        /// <param name="options">The JSON serializer options.</param>
        /// <returns>A task representing the asynchronous serialization operation.</returns>
        public static Task SerializeAsyncEnumerableAsync(Stream stream, object asyncEnumerable, Type elementType, JsonSerializerOptions options)
        {
            var serializer = _asyncEnumerableSerializers.GetOrAdd(elementType, CompileAsyncEnumerableSerializer);
            return serializer(stream, asyncEnumerable, options);
        }

        private static Func<Stream, object, JsonSerializerOptions, Task> CompileAsyncEnumerableSerializer(Type elementType)
        {
            var method = typeof(JsonSerializer)
                .GetMethods()
                .First(m => m.Name == nameof(JsonSerializer.SerializeAsync)
                            && m.IsGenericMethod
                            && m.GetParameters().Length == 4
                            && m.GetParameters()[0].ParameterType == typeof(Stream)
                            && m.GetParameters()[3].ParameterType == typeof(CancellationToken));

            var generic = method.MakeGenericMethod(elementType);

            var streamParam = Expression.Parameter(typeof(Stream), "stream");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

            var enumerableType = typeof(IAsyncEnumerable<>).MakeGenericType(elementType);
            var valueCast = Expression.Convert(valueParam, enumerableType);
            var cancellationToken = Expression.Constant(CancellationToken.None, typeof(CancellationToken));

            var call = Expression.Call(generic, streamParam, valueCast, optionsParam, cancellationToken);
            var lambda = Expression.Lambda<Func<Stream, object, JsonSerializerOptions, Task>>(call, streamParam, valueParam, optionsParam);
            return lambda.Compile();
        }

        private static Func<object, object?[], object?> Compile(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object?[]), "args");

            var targetCast = Expression.Convert(targetParam, method.DeclaringType!);

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = Expression.Constant(i);
                var arrayAccess = Expression.ArrayAccess(argsParam, index);
                argExpressions[i] = Expression.Convert(arrayAccess, parameters[i].ParameterType);
            }

            var call = Expression.Call(targetCast, method, argExpressions);

            Expression body;
            if (method.ReturnType == typeof(void))
            {
                body = Expression.Block(call, Expression.Constant(null, typeof(object)));
            }
            else
            {
                body = Expression.Convert(call, typeof(object));
            }

            var lambda = Expression.Lambda<Func<object, object?[], object?>>(body, targetParam, argsParam);
            return lambda.Compile();
        }

        private static Func<Task, object?> CompileTaskResultExtractor(Type taskType)
        {
            var taskParam = Expression.Parameter(typeof(Task), "task");
            var cast = Expression.Convert(taskParam, taskType);
            var resultProperty = taskType.GetProperty("Result")!;
            var resultAccess = Expression.Property(cast, resultProperty);
            var body = Expression.Convert(resultAccess, typeof(object));
            var lambda = Expression.Lambda<Func<Task, object?>>(body, taskParam);
            return lambda.Compile();
        }

        private static Func<object, object?> CompileValueTaskResultExtractor(Type valueTaskType)
        {
            var vtParam = Expression.Parameter(typeof(object), "valueTask");
            var cast = Expression.Convert(vtParam, valueTaskType);
            var resultProperty = valueTaskType.GetProperty("Result")!;
            var resultAccess = Expression.Property(cast, resultProperty);
            var body = Expression.Convert(resultAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object?>>(body, vtParam);
            return lambda.Compile();
        }
    }
}

