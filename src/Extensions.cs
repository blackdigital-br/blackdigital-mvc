namespace BlackDigital.AspNet
{
    internal static class Extensions
    {
        public static string ToStringInvariant(this int i)
        => i.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
