
namespace BlackDigital.AspNet.Infrastructures
{
    public interface IEmailService
    {
        Task SendEmailAsync(Email email);
    }
}
