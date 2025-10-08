using BlackDigital.Mvc.Example.Models;
using BlackDigital.Mvc.Rest.Trasnforms;

namespace BlackDigital.Mvc.Example.Transform
{
    public class SaveUserIdTransformRule : TransformRule<SaveUser, int>
    {
        public override int Transform(SaveUser? value)
        {
            return value?.Id ?? 12;
        }
    }
}
