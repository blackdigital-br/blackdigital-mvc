using BlackDigital.Mvc.Example.Models;
using BlackDigital.Mvc.Rest.Trasnforms;

namespace BlackDigital.Mvc.Example.Transform
{
    public class OldSaveUsertoSaveUserTransformRule : TransformRule<OldSaveUser, SaveUser>
    {
        public override SaveUser? Transform(OldSaveUser? value)
        {
            if (value == null)
                return null;

            return new SaveUser
            {
                Name = value.N,
                Email = value.E,
                Password = value.P
            };
        }
    }
}
