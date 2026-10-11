# Email

🇧🇷 Português | [🇺🇸 English](email.md)

O módulo de Email fornece um serviço de email SMTP (via MailKit) com suporte opcional a templates HTML. Ele suporta múltiplos provedores com configuração de fallback.

## Configuração

### appsettings.json

```json
{
  "Email": {
    "From": "no-reply@exemplo.com",
    "FromName": "Meu App",
    "Provider": "SendGrid",
    "Providers": {
      "SendGrid": {
        "SmtpServer": "smtp.sendgrid.net",
        "SmtpPort": 587,
        "Username": "apikey",
        "Password": "sua-api-key-do-sendgrid",
        "UseSsl": true
      },
      "Fallback": {
        "SmtpServer": "smtp.gmail.com",
        "SmtpPort": 587,
        "Username": "voce@gmail.com",
        "Password": "sua-senha-de-app",
        "UseSsl": true
      }
    }
  }
}
```

O campo `Provider` seleciona o provedor ativo em `Providers`. Se o provedor ativo falhar no envio, o serviço automaticamente faz fallback para os outros provedores configurados.

### Registrar o serviço

```csharp
using BlackDigital.AspNet.Infrastructures;

// Registra o serviço de email SMTP (lê a configuração da seção "Email")
builder.Services.AddSmtpService(builder.Configuration);

// Opcional: registra o serviço de email com template HTML
builder.Services.AddHtmlTemplateEmailService();
```

Você também pode registrar uma implementação customizada de `IEmailService`:

```csharp
builder.Services.AddEmailService<MeuEmailServiceCustomizado>();
```

## Enviando um Email Simples

Injete `IEmailService` e construa um email com a API fluente `Email`:

```csharp
public class NotificationService
{
    private readonly IEmailService _emailService;

    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendWelcomeAsync(string toEmail, string name)
    {
        var email = Email.Create()
            .To(toEmail, name)
            .WithSubject("Bem-vindo ao Meu App")
            .WithBody($"<h1>Olá, {name}!</h1><p>Obrigado por se cadastrar.</p>", isHtml: true);

        await _emailService.SendAsync(email);
    }
}
```

### API Fluente Email

| Método | Descrição |
|--------|-----------|
| `Email.Create()` | Inicia um novo email |
| `.To(address, name?)` | Adiciona um destinatário |
| `.WithSubject(subject)` | Define o assunto |
| `.WithBody(body, isHtml)` | Define o conteúdo do corpo |
| `.From(address, name?)` | Sobrescreve o remetente padrão |
| `.Cc(address, name?)` | Adiciona um destinatário em cópia |
| `.Bcc(address, name?)` | Adiciona um destinatário em cópia oculta |
| `.AddAttachment(stream, fileName, contentType)` | Anexa um arquivo |

## Emails com Template HTML

Para emails transacionais que compartilham um layout comum, use `ITemplateEmailService` com templates HTML.

### Localização dos templates

Templates são carregados do diretório `templates/email/` na raiz da aplicação. Cada template é um arquivo HTML com placeholders `{{ chave }}`.

Exemplo `templates/email/welcome.html`:

```html
<!DOCTYPE html>
<html>
<body>
  <h1>Bem-vindo, {{ userName }}!</h1>
  <p>Obrigado por se cadastrar no {{ appName }}.</p>
  <p>Clique <a href="{{ confirmUrl }}">aqui</a> para confirmar sua conta.</p>
</body>
</html>
```

### Enviando um email com template

```csharp
public class NotificationService
{
    private readonly ITemplateEmailService _templateEmail;

    public NotificationService(ITemplateEmailService templateEmail)
    {
        _templateEmail = templateEmail;
    }

    public async Task SendWelcomeAsync(string toEmail, string userName, string confirmUrl)
    {
        var email = TemplateEmail.Create("welcome")
            .To(toEmail)
            .WithSubject("Bem-vindo ao Meu App")
            .AddParameter("userName", userName)
            .AddParameter("appName", "Meu App")
            .AddParameter("confirmUrl", confirmUrl);

        await _templateEmail.SendAsync(email);
    }
}
```

O serviço carrega `templates/email/welcome.html`, substitui cada `{{ chave }}` pelo valor do parâmetro correspondente e envia o HTML resultante.

### API Fluente TemplateEmail

Herda todos os métodos de `Email` e adiciona:

| Método | Descrição |
|--------|-----------|
| `TemplateEmail.Create(templateName)` | Inicia um novo email com template usando `templates/email/{templateName}.html` |
| `.AddParameter(key, value)` | Adiciona um valor de placeholder (`{{ chave }}` → valor) |

## Membros do IEmailService

| Membro | Descrição |
|--------|-----------|
| `SendAsync(Email email)` | Envia o email usando o provedor configurado (com fallback) |

## Documentação Relacionada

- [Documentação Principal](../README.pt.md)
