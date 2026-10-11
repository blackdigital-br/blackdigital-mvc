namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Base class for transform rules that operate on untyped objects.
    /// Provides default pass-through implementations so subclasses only need
    /// to override the members relevant to their behavior.
    /// </summary>
    public abstract class TransformRule : ITransformRule
    {
        /// <summary>
        /// Gets the expected input type for the transform.
        /// Returns <c>null</c> by default, indicating any input is accepted.
        /// </summary>
        public virtual Type? InputType => null;

        /// <summary>
        /// Gets the output type produced by the transform.
        /// Returns <c>null</c> by default, indicating the output type is not fixed.
        /// </summary>
        public virtual Type? OutputType => null;

        /// <summary>
        /// Transforms the specified value synchronously.
        /// The default implementation returns the value unchanged.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        public virtual object? Transform(object? value)
        {
            return value;
        }

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// The default implementation delegates to <see cref="Transform(object?)"/>.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        public virtual Task<object?> TransformAsync(object? value)
        {
            return Task.FromResult(Transform(value));
        }
    }

    /// <summary>
    /// Base class for transform rules that operate on a single input/output type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The input and output type of the transform.</typeparam>
    public abstract class TransformRule<T> : ITransformRule<T>
    {
        /// <summary>
        /// Gets the expected input type for the transform.
        /// </summary>
        public virtual Type? InputType => typeof(T);

        /// <summary>
        /// Gets the output type produced by the transform.
        /// </summary>
        public virtual Type? OutputType => typeof(T);

        /// <summary>
        /// Transforms the specified value synchronously.
        /// The default implementation returns the value unchanged.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        public virtual T? Transform(T? value)
        {
            return value;
        }

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// The default implementation delegates to <see cref="Transform(T?)"/>.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        public virtual Task<T?> TransformAsync(T? value)
        {
            return Task.FromResult(Transform(value));
        }

        /// <summary>
        /// Explicit interface implementation that casts the value to
        /// <typeparamref name="T"/> and delegates to the typed overload.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        object? ITransformRule.Transform(object? value)
            => Transform((T?)value);

        /// <summary>
        /// Explicit interface implementation that casts the value to
        /// <typeparamref name="T"/> and delegates to the typed overload.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        async Task<object?> ITransformRule.TransformAsync(object? value)
            => await TransformAsync((T?)value).ConfigureAwait(false);
    }

    /// <summary>
    /// Base class for transform rules that convert a value from
    /// <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TIn">The input type of the transform.</typeparam>
    /// <typeparam name="TOut">The output type of the transform.</typeparam>
    public abstract class TransformRule<TIn, TOut> : ITransformRule<TIn, TOut>
    {
        /// <summary>
        /// Gets the expected input type for the transform.
        /// </summary>
        public virtual Type? InputType => typeof(TIn);

        /// <summary>
        /// Gets the output type produced by the transform.
        /// </summary>
        public virtual Type? OutputType => typeof(TOut);

        /// <summary>
        /// Transforms the specified value synchronously.
        /// The default implementation returns the default value of
        /// <typeparamref name="TOut"/>.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        public virtual TOut? Transform(TIn? value)
        {
            return default;
        }

        /// <summary>
        /// Transforms the specified value asynchronously.
        /// The default implementation delegates to <see cref="Transform(TIn?)"/>.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        public virtual Task<TOut?> TransformAsync(TIn? value)
        {
            return Task.FromResult(Transform(value));
        }

        /// <summary>
        /// Explicit interface implementation that casts the value to
        /// <typeparamref name="TIn"/> and delegates to the typed overload.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        object? ITransformRule.Transform(object? value)
            => Transform((TIn?)value);

        /// <summary>
        /// Explicit interface implementation that casts the value to
        /// <typeparamref name="TIn"/> and delegates to the typed overload.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <returns>A task that yields the transformed value.</returns>
        async Task<object?> ITransformRule.TransformAsync(object? value)
            => await TransformAsync((TIn?)value).ConfigureAwait(false);
    }
}
