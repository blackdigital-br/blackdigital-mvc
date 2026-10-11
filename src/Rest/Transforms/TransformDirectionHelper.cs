
namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Helper extension methods for inspecting and enumerating
    /// <see cref="TransformDirection"/> flag values.
    /// </summary>
    public static class TransformDirectionHelper
    {
        /// <summary>
        /// Determines whether the direction includes the <see cref="TransformDirection.Input"/> flag.
        /// </summary>
        /// <param name="direction">The direction to inspect.</param>
        /// <returns><c>true</c> when the input flag is set; otherwise, <c>false</c>.</returns>
        public static bool HasInput(this TransformDirection direction)
            => (direction & TransformDirection.Input) == TransformDirection.Input;

        /// <summary>
        /// Determines whether the direction includes the <see cref="TransformDirection.Output"/> flag.
        /// </summary>
        /// <param name="direction">The direction to inspect.</param>
        /// <returns><c>true</c> when the output flag is set; otherwise, <c>false</c>.</returns>
        public static bool HasOutput(this TransformDirection direction)
            => (direction & TransformDirection.Output) == TransformDirection.Output;

        /// <summary>
        /// Enumerates the individual direction flags contained in the value.
        /// Yields <see cref="TransformDirection.Input"/> and/or
        /// <see cref="TransformDirection.Output"/> based on the set flags.
        /// </summary>
        /// <param name="direction">The direction to enumerate.</param>
        /// <returns>A sequence of the set direction flags.</returns>
        public static IEnumerable<TransformDirection> Enumerate(this TransformDirection direction)
        {
            if (direction.HasInput())
                yield return TransformDirection.Input;

            if (direction.HasOutput())
                yield return TransformDirection.Output;
        }
    }
}
