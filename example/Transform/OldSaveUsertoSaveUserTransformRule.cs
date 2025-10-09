using BlackDigital.AspNet.Example.Models;
using BlackDigital.AspNet.Rest.Trasnforms;

namespace BlackDigital.AspNet.Example.Transform
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
