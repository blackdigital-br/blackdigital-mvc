namespace BlackDigital.Mvc.Infrastructures
{
    public class Email
    {
        public string? Provider { get; set; }

        public Dictionary<string, string> Tos { get; set; } = new();

        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsHtml { get; set; } = true;

        public Email WithProvider(string provider)
        {
            Provider = provider;
            return this;
        }

        public Email To(string email, string? name = null)
        {
            Tos[email] = name ?? email;
            return this;
        }

        public Email To(IEnumerable<string> emails)
        {
            foreach (var email in emails)
                Tos[email] = email;

            return this;
        }

        public Email WithSubject(string subject)
        {
            Subject = subject;
            return this;
        }

        public Email WithBody(string body, bool isHtml = true)
        {
            Body = body;
            IsHtml = isHtml;
            return this;
        }

        public static Email Create()
            => new();

        public static Email Create(string to, string subject, string body, bool isHtml = true, string? provider = null)
        {
            return new Email
            {
                Provider = provider,
                Tos = new Dictionary<string, string> { { to, to } },
                Subject = subject,
                Body = body,
                IsHtml = isHtml
            };
        }

        public static Email Create(Dictionary<string, string> tos, string subject, string body, bool isHtml = true, string? provider = null)
        {
            return new Email
            {
                Provider = provider,
                Tos = tos,
                Subject = subject,
                Body = body,
                IsHtml = isHtml
            };
        }
    }
}
