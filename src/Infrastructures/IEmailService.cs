
namespace BlackDigital.AspNet.Infrastructures
{
    public interface IEmailService
    {
        Task SendEmailAsync(Email email);

        Task SendEmailAsync(Func<Email, Email> sendEmail);
    }
}
