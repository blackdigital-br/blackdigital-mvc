
namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Defines a transform rule that converts a value from an input type to an
    /// output type. Rules are registered with a <see cref="TransformKey"/> and
    /// executed by the <see cref="TransformManager"/>.
    /// </summary>
    public interface ITransformRule
    {
        /// <summary>
        /// Gets the expected input type for the transform, or <c>null</c> when
        /// the rule accepts any input.
        /// </summary>
        Type? InputType { get; }

        /// <summary>
        /// Gets the output type produced by the transform, or <c>null</c> when
        /// the output type is not fixed.
        /// </summary>
        Type? OutputType { get; }

        /// <summary>
        /// Transforms the specified value synchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        object? Transform(object? value);

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        Task<object?> TransformAsync(object? value);
    }

    /// <summary>
    /// Defines a transform rule that operates on a single input/output type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The input and output type of the transform.</typeparam>
    public interface ITransformRule<T> : ITransformRule
    {
        /// <summary>
        /// Transforms the specified value synchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        T? Transform(T? value);

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        Task<T?> TransformAsync(T? value);
    }

    /// <summary>
    /// Defines a transform rule that converts a value from
    /// <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TIn">The input type of the transform.</typeparam>
    /// <typeparam name="TOut">The output type of the transform.</typeparam>
    public interface ITransformRule<TIn, TOut> : ITransformRule
    {
        /// <summary>
        /// Transforms the specified value synchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        TOut? Transform(TIn? value);

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        Task<TOut?> TransformAsync(TIn? value);
    }
}
