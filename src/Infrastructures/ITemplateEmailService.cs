namespace BlackDigital.AspNet.Infrastructures
{
    public interface ITemplateEmailService
    {
        Task SendTemplateEmailAsync(TemplateEmail email);

        Task SendTemplateEmailAsync(Func<TemplateEmail, TemplateEmail> emailBuilder);
    }
}