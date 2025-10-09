
namespace BlackDigital.AspNet.Rest.Trasnforms
{
    public static class TransformDirectionHelper
    {
        public static bool HasInput(this TransformDirection direction)
            => (direction & TransformDirection.Input) == TransformDirection.Input;

        public static bool HasOutput(this TransformDirection direction)
            => (direction & TransformDirection.Output) == TransformDirection.Output;

        public static IEnumerable<TransformDirection> Enumerate(this TransformDirection direction)
        {
            if (direction.HasInput())
                yield return TransformDirection.Input;

            if (direction.HasOutput())
                yield return TransformDirection.Output;
        }
    }
}
