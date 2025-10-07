
namespace BlackDigital.Mvc.Services
{
    public interface ITemplateEmailService
    {
        Task SendTemplateEmailAsync(EmailTemplate emailTemplate);
    }
}
