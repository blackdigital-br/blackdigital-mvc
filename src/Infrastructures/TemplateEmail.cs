namespace BlackDigital.AspNet.Infrastructures
{
    public class TemplateEmail : Email
    {
        public string TemplateName { get; set; } = string.Empty;

        public Dictionary<string, string> Parameters { get; set; } = new();

        public TemplateEmail UseTemplate(string templateName)
        {
            TemplateName = templateName;
            return this;
        }

        public TemplateEmail AddParameter(string name, string value)
        {
            Parameters[name] = value;
            return this;
        }

        public TemplateEmail AddParameters(Dictionary<string, string> parameters, bool overwrite = true)
        {
            foreach (var parameter in parameters)
            {
                if (overwrite || !Parameters.ContainsKey(parameter.Key))
                    Parameters[parameter.Key] = parameter.Value;
            }

            return this;
        }
    }
}