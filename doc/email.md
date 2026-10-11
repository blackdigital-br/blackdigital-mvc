# Email

[🇧🇷 Português](email.pt.md) | 🇺🇸 English

The Email module provides an SMTP email service (via MailKit) with optional HTML template support. It supports multiple providers with fallback configuration.

## Configuration

### appsettings.json

```json
{
  "Email": {
    "From": "no-reply@example.com",
    "FromName": "My App",
    "Provider": "SendGrid",
    "Providers": {
      "SendGrid": {
        "SmtpServer": "smtp.sendgrid.net",
        "SmtpPort": 587,
        "Username": "apikey",
        "Password": "your-sendgrid-api-key",
        "UseSsl": true
      },
      "Fallback": {
        "SmtpServer": "smtp.gmail.com",
        "SmtpPort": 587,
        "Username": "you@gmail.com",
        "Password": "your-app-password",
        "UseSsl": true
      }
    }
  }
}
```

The `Provider` field selects the active provider from `Providers`. If the active provider fails to send, the service automatically falls back to the other configured providers.

### Register the service

```csharp
using BlackDigital.AspNet.Infrastructures;

// Register SMTP email service (reads configuration from the "Email" section)
builder.Services.AddSmtpService(builder.Configuration);

// Optional: register HTML template email service
builder.Services.AddHtmlTemplateEmailService();
```

You can also register a custom `IEmailService` implementation:

```csharp
builder.Services.AddEmailService<MyCustomEmailService>();
```

## Sending a Plain Email

Inject `IEmailService` and build an email with the fluent `Email` API:

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
            .WithSubject("Welcome to My App")
            .WithBody($"<h1>Hello, {name}!</h1><p>Thanks for signing up.</p>", isHtml: true);

        await _emailService.SendAsync(email);
    }
}
```

### Email Fluent API

| Method | Description |
|--------|-------------|
| `Email.Create()` | Starts a new email |
| `.To(address, name?)` | Adds a recipient |
| `.WithSubject(subject)` | Sets the subject |
| `.WithBody(body, isHtml)` | Sets the body content |
| `.From(address, name?)` | Overrides the default sender |
| `.Cc(address, name?)` | Adds a CC recipient |
| `.Bcc(address, name?)` | Adds a BCC recipient |
| `.AddAttachment(stream, fileName, contentType)` | Attaches a file |

## HTML Template Emails

For transactional emails that share a common layout, use `ITemplateEmailService` with HTML templates.

### Template location

Templates are loaded from the `templates/email/` directory at the application root. Each template is an HTML file with `{{ key }}` placeholders.

Example `templates/email/welcome.html`:

```html
<!DOCTYPE html>
<html>
<body>
  <h1>Welcome, {{ userName }}!</h1>
  <p>Thank you for registering at {{ appName }}.</p>
  <p>Click <a href="{{ confirmUrl }}">here</a> to confirm your account.</p>
</body>
</html>
```

### Sending a template email

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
            .WithSubject("Welcome to My App")
            .AddParameter("userName", userName)
            .AddParameter("appName", "My App")
            .AddParameter("confirmUrl", confirmUrl);

        await _templateEmail.SendAsync(email);
    }
}
```

The service loads `templates/email/welcome.html`, replaces each `{{ key }}` with the corresponding parameter value, and sends the resulting HTML.

### TemplateEmail Fluent API

Inherits all methods from `Email` and adds:

| Method | Description |
|--------|-------------|
| `TemplateEmail.Create(templateName)` | Starts a new template email using `templates/email/{templateName}.html` |
| `.AddParameter(key, value)` | Adds a placeholder value (`{{ key }}` → value) |

## IEmailService Members

| Member | Description |
|--------|-------------|
| `SendAsync(Email email)` | Sends the email using the configured provider (with fallback) |

## Related Documentation

- [Main Documentation](../README.md)
