using BlackDigital.AspNet.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BlackDigital.AspNet.Infrastructures
{
    public class HtmlTemplateEmailService : BaseTemplateEmailService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<HtmlTemplateEmailService> _logger;
        private readonly string _templateBasePath;

        public HtmlTemplateEmailService(IEmailService emailService,
                                        ILogger<HtmlTemplateEmailService> logger,
                                        IWebHostEnvironment environment)
        {
            _emailService = emailService;
            _logger = logger;

            // Templates live under the host's content root (e.g. templates/email).
            var contentRoot = environment?.ContentRootPath ?? Directory.GetCurrentDirectory();
            _templateBasePath = Path.Combine(contentRoot, "templates", "email");
        }

        public override async Task SendTemplateEmailAsync(TemplateEmail email)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(email);

                // Load the template
                email.Body = await LoadTemplateAsync(email.TemplateName);

                // Process the template with parameters
                email.Body = ProcessTemplate(email.Body, email.Parameters);

                // Send the email
                await _emailService.SendEmailAsync(email);

                _logger.LogInformation("Template email sent successfully. Template: {TemplateName}, To: {To}", email.TemplateName, string.Join(", ", email.Tos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email. Template: {TemplateName}, To: {To}", email.TemplateName, string.Join(", ", email.Tos));
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

        // Matches {{ key }} placeholders, capturing the trimmed key name.
        // Compiled once and reused across all template renders to avoid
        // recompiling a regex per parameter on every send.
        private static readonly Regex TemplateVariableRegex =
            new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private string ProcessTemplate(string templateContent, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                // Remove all template variables if no parameters provided
                return TemplateVariableRegex.Replace(templateContent, string.Empty);
            }

            // Replace placeholders in a single pass, looking up each captured
            // key in the parameters dictionary. Unmatched keys are removed.
            return TemplateVariableRegex.Replace(templateContent, match =>
            {
                var key = match.Groups[1].Value;
                return parameters.TryGetValue(key, out var value) ? (value ?? string.Empty) : string.Empty;
            });
        }
    }
}