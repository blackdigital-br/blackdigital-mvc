using BlackDigital.AspNet.Example.Models;
using BlackDigital.AspNet.Rest.Trasnforms;

namespace BlackDigital.AspNet.Example.Transform
{
    public class SaveUserTransformRule : TransformRule<SaveUser>
    {
        public SaveUserTransformRule(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override SaveUser? Transform(SaveUser? value)
        {
            value.Email += $".{Name}";
            return base.Transform(value);
        }
    }
}
