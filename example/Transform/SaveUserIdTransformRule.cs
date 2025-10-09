using BlackDigital.AspNet.Example.Models;
using BlackDigital.AspNet.Rest.Trasnforms;

namespace BlackDigital.AspNet.Example.Transform
{
    public class SaveUserIdTransformRule : TransformRule<SaveUser, int>
    {
        public override int Transform(SaveUser? value)
        {
            return value?.Id ?? 12;
        }
    }
}
