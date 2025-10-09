using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BlackDigital.AspNet.Infrastructures
{
    /// <summary>
    /// Implementação do serviço de email usando protocolo SMTP.
    /// Suporta múltiplos provedores de email (Gmail, Outlook, etc.) com configuração flexível e fallback.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Nome do provedor de email a ser usado. Se null ou vazio, usa a configuração padrão.
        /// </summary>
        public string? Provider { get; protected set; }

        /// <summary>
        /// Inicializa uma nova instância do SmtpEmailService.
        /// </summary>
        /// <param name="logger">Logger para registrar informações e erros</param>
        /// <param name="configuration">Configuração da aplicação contendo as configurações de email</param>
        public SmtpEmailService(ILogger<SmtpEmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Define qual provedor de email usar (ex: "Outlook", "Gmail"). 
        /// Se null ou vazio, usa a configuração padrão.
        /// </summary>
        /// <param name="providerName">Nome do provedor ou null para usar configuração padrão</param>
        public void SetProvider(string? providerName)
        {
            Provider = providerName;
            _logger.LogInformation("Email provider changed to: {Provider}",
                string.IsNullOrEmpty(providerName) ? "Default" : providerName);
        }

        /// <summary>
        /// Envia um email de forma assíncrona usando protocolo SMTP.
        /// Suporta configuração específica por provedor com fallback para configuração padrão.
        /// </summary>
        /// <param name="email">Objeto Email contendo destinatários, assunto, corpo e configurações</param>
        /// <returns>Task representando a operação assíncrona de envio</returns>
        /// <exception cref="Exception">Lançada quando ocorre erro durante o envio do email</exception>
        public async Task SendEmailAsync(Email email)
        {
            try
            {
                var provider = email?.Provider ?? Provider;

                // Determina o prefixo da configuração baseado no provedor
                var configPrefix = string.IsNullOrEmpty(provider) ? "Email" : $"Email:{provider}";

                // Lê as configurações com fallback para configuração padrão
                var host = GetConfigValue("Host", configPrefix);
                var portStr = GetConfigValue("Port", configPrefix) ?? "587";
                var port = int.Parse(portStr);
                var user = GetConfigValue("User", configPrefix);
                var password = GetConfigValue("Password", configPrefix);
                var fromEmail = GetConfigValue("From", configPrefix);
                var fromName = GetConfigValue("Name", configPrefix);

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogWarning("Email configuration is incomplete. Email not sent to {Recipients}", string.Join(", ", email.Tos.Keys));
                    return;
                }

                using var client = new SmtpClient(host, port);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(user, password);

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);

                foreach (var to in email.Tos)
                {
                    message.To.Add(new MailAddress(to.Key, to.Value));
                }
                
                message.Subject = email.Subject;
                message.Body = email.Body;
                message.IsBodyHtml = email.IsHtml;

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to with subject: {Subject}", email.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to with subject: {Subject}", email.Subject);
                throw;
            }
        }

        /// <summary>
        /// Obtém um valor de configuração com fallback para a configuração padrão.
        /// Primeiro tenta ler do provedor específico, se não encontrar, usa a configuração padrão.
        /// </summary>
        /// <param name="key">Chave da configuração (ex: "Host", "Port")</param>
        /// <param name="configPrefix">Prefixo da configuração (ex: "Email" ou "Email:Outlook")</param>
        /// <returns>Valor da configuração ou null se não encontrado</returns>
        private string? GetConfigValue(string key, string configPrefix)
        {
            var fullKey = $"{configPrefix}:{key}";
            var value = _configuration[fullKey];

            // Se não encontrou e não é a configuração padrão, tenta fallback
            if (string.IsNullOrEmpty(value) && configPrefix != "Email")
            {
                var defaultKey = $"Email:{key}";
                value = _configuration[defaultKey];

                if (!string.IsNullOrEmpty(value))
                {
                    _logger.LogDebug("Using fallback configuration for {Key}: Email:{Key} (provider {Provider} not configured)",
                        key, key, Provider);
                }
            }

            return value;
        }
    }
}
