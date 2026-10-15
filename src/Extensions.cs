namespace BlackDigital.AspNet
{
    internal static class Extensions
    {
        /// <summary>
        /// Converts the integer to its string representation using the invariant culture.
        /// </summary>
        /// <param name="i">The integer to convert.</param>
        /// <returns>The string representation of the integer in the invariant culture.</returns>
        public static string ToStringInvariant(this int i)
        => i.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
