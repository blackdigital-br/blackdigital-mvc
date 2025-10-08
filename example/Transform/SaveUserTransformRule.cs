using BlackDigital.Mvc.Example.Models;
using BlackDigital.Mvc.Rest.Trasnforms;

namespace BlackDigital.Mvc.Example.Transform
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
