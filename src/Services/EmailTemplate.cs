
using BlackDigital.AspNet.Infrastructures;

namespace BlackDigital.AspNet.Services
{
    public class EmailTemplate
    {
        public string? Provider { get; private set; }

        public string TemplateName { get; private set; }

        public Dictionary<string, string> Tos { get; private set; } = new();

        public string Subject { get; private set; }

        public Dictionary<string, string> Parameters { get; private set; } = new();

        public EmailTemplate WithTemplate(string templateName)
        {
            TemplateName = templateName;
            return this;
        }

        public EmailTemplate WithProvider(string provider)
        {
            Provider = provider;
            return this;
        }

        public EmailTemplate To(string email, string? name = null)
        {
            Tos[email] = name ?? email;
            return this;
        }

        public EmailTemplate To(IEnumerable<string> emails)
        {
            foreach (var email in emails)
                Tos[email] = email;

            return this;
        }

        public EmailTemplate WithSubject(string subject)
        {
            Subject = subject;
            return this;
        }

        public EmailTemplate WithParameter(string key, string value)
        {
            Parameters.Add(key, value);
            return this;
        }

        public static EmailTemplate Create()
            => new();

        public static EmailTemplate Create(string templateName)
            => Create().WithTemplate(templateName);

        public static EmailTemplate Create(string templateName, string subject, IEnumerable<string> emails)
            => Create(templateName).WithSubject(subject).To(emails);
    }
}
