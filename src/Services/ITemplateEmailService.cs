
namespace BlackDigital.AspNet.Services
{
    public interface ITemplateEmailService
    {
        Task SendTemplateEmailAsync(EmailTemplate emailTemplate);
    }
}
