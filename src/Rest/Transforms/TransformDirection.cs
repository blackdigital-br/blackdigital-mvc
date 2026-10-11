
namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Specifies the direction in which a transform rule applies.
    /// Values are flags so a single rule can cover both input and output.
    /// </summary>
    [Flags]
    public enum TransformDirection
    {
        /// <summary>
        /// The rule applies to incoming request data (deserialization/input).
        /// </summary>
        Input = 1,

        /// <summary>
        /// The rule applies to outgoing response data (serialization/output).
        /// </summary>
        Output = 2,

        /// <summary>
        /// The rule applies to both input and output.
        /// </summary>
        Both = Input | Output
    }
}
