
namespace BlackDigital.Mvc.Infrastructures
{
    public interface IEmailService
    {
        Task SendEmailAsync(Email email);
    }
}
