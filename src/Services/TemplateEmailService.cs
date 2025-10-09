
using BlackDigital.AspNet.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BlackDigital.AspNet.Services
{
    public class TemplateEmailService : ITemplateEmailService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<TemplateEmailService> _logger;
        private readonly string _templateBasePath;

        public TemplateEmailService(IEmailService emailService, ILogger<TemplateEmailService> logger, IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;

            // Get the base path for templates from configuration or use default
            var contentRoot = configuration["ContentRoot"] ?? Directory.GetCurrentDirectory();
            _templateBasePath = Path.Combine(contentRoot, "templates", "email");
        }

        public async Task SendTemplateEmailAsync(EmailTemplate emailTemplate)
        {
            try
            {
                // Load the template
                var templateContent = await LoadTemplateAsync(emailTemplate.TemplateName);

                // Process the template with parameters
                var processedContent = ProcessTemplate(templateContent, emailTemplate.Parameters);

                Email email = Email.Create(emailTemplate.Tos, emailTemplate.Subject, processedContent, true);

                // Send the email
                await _emailService.SendEmailAsync(email);

                _logger.LogInformation("Template email sent successfully. Template: {TemplateName}", emailTemplate.TemplateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email. Template: {TemplateName}", emailTemplate.TemplateName);
                throw;
            }
        }

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            // Validate template name (no path traversal, empty or whitespace)
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("Invalid template name", nameof(templateName));
            }

            // Check for path traversal attempts
            if (templateName.Contains("..") || templateName.Contains("/") || templateName.Contains("\\"))
            {
                throw new ArgumentException("Invalid template name", nameof(templateName));
            }

            // Ensure template name is safe (no path traversal)
            var safeTemplateName = Path.GetFileName(templateName);
            if (string.IsNullOrEmpty(safeTemplateName))
            {
                throw new ArgumentException("Invalid template name", nameof(templateName));
            }

            // Add .html extension if not present
            if (!safeTemplateName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                safeTemplateName += ".html";
            }

            var templatePath = Path.Combine(_templateBasePath, safeTemplateName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found: {safeTemplateName}");
            }

            return await File.ReadAllTextAsync(templatePath);
        }

        private string ProcessTemplate(string templateContent, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                // Remove all template variables if no parameters provided
                return Regex.Replace(templateContent, @"\{\{[^}]+\}\}", string.Empty);
            }

            var processedContent = templateContent;

            // Replace parameters that exist in the dictionary
            foreach (var parameter in parameters)
            {
                var pattern = $@"\{{\{{\s*{Regex.Escape(parameter.Key)}\s*\}}\}}";
                processedContent = Regex.Replace(processedContent, pattern, parameter.Value ?? string.Empty);
            }

            // Remove any remaining template variables that weren't provided
            processedContent = Regex.Replace(processedContent, @"\{\{[^}]+\}\}", string.Empty);

            return processedContent;
        }
    }
}
