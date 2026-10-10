namespace BlackDigital.AspNet.Infrastructures
{
    public abstract class BaseTemplateEmailService : ITemplateEmailService
    {
        public abstract Task SendTemplateEmailAsync(TemplateEmail email);

        public Task SendTemplateEmailAsync(Func<TemplateEmail, TemplateEmail> emailBuilder)
        {
            var email = emailBuilder(new TemplateEmail());
            return SendTemplateEmailAsync(email);
        }
    }
}