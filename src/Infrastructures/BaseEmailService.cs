namespace BlackDigital.AspNet.Infrastructures
{
    public abstract class BaseEmailService : IEmailService
    {
        public abstract Task SendEmailAsync(Email email);

        public async Task SendEmailAsync(Func<Email, Email> sendEmail)
        {
            var email = sendEmail(Email.Create());
            await SendEmailAsync(email);
        }
    }
}