using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BlackDigital.AspNet.Infrastructures
{
    /// <summary>
    /// Implementação do serviço de email usando protocolo SMTP via MailKit.
    /// Suporta múltiplos provedores de email (Gmail, Outlook, etc.) com configuração flexível e fallback.
    /// </summary>
    public class SmtpEmailService : BaseEmailService
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
        /// Envia um email de forma assíncrona usando protocolo SMTP via MailKit.
        /// Suporta configuração específica por provedor com fallback para configuração padrão.
        /// </summary>
        /// <param name="email">Objeto Email contendo destinatários, assunto, corpo e configurações</param>
        /// <returns>Task representando a operação assíncrona de envio</returns>
        public override async Task SendEmailAsync(Email email)
        {
            try
            {
                var provider = email.Provider ?? Provider;

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
                var sslRaw = GetConfigValue("UseSsl", configPrefix);
                var startTlsRaw = GetConfigValue("UseStartTls", configPrefix);

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogWarning("Email configuration is incomplete. Email not sent to {Recipients}", string.Join(", ", email.Tos.Keys));
                    return;
                }

                using var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName ?? fromEmail, fromEmail));

                foreach (var to in email.Tos)
                {
                    message.To.Add(new MailboxAddress(to.Value, to.Key));
                }

                message.Subject = email.Subject;

                var bodyBuilder = new BodyBuilder();
                if (email.IsHtml)
                    bodyBuilder.HtmlBody = email.Body;
                else
                    bodyBuilder.TextBody = email.Body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Decide secure socket option: defaults to STARTTLS on port 587,
                // implicit SSL on 465, and auto-negotiation otherwise. The caller
                // can override per-provider via UseSsl / UseStartTls flags.
                var secureSocket = ResolveSecureSocketOption(port, sslRaw, startTlsRaw);

                await client.ConnectAsync(host, port, secureSocket);
                await client.AuthenticateAsync(user, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);

                _logger.LogInformation("Email sent successfully to with subject: {Subject}", email.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to with subject: {Subject}", email.Subject);
                throw;
            }
        }

        /// <summary>
        /// Resolves the <see cref="SecureSocketOptions"/> to use for the SMTP
        /// connection. Explicit per-provider flags take precedence; otherwise the
        /// common defaults (implicit SSL on 465, STARTTLS on 587, auto otherwise)
        /// are applied.
        /// </summary>
        private static SecureSocketOptions ResolveSecureSocketOption(int port, string? useSslRaw, string? useStartTlsRaw)
        {
            if (bool.TryParse(useSslRaw, out var useSsl) && useSsl)
                return SecureSocketOptions.SslOnConnect;

            if (bool.TryParse(useStartTlsRaw, out var useStartTls) && useStartTls)
                return SecureSocketOptions.StartTls;

            if (port == 465)
                return SecureSocketOptions.SslOnConnect;

            if (port == 587)
                return SecureSocketOptions.StartTls;

            return SecureSocketOptions.Auto;
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
