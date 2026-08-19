# Send Email from Your App

Configure your application's outgoing email to use host **`localhost`**, port **`25`**, **no credentials, no encryption**. That's the whole recipe — here it is in common stacks.

!!! note
    Using the [Docker image](docker.md)? The default SMTP port there is **2525**, not 25.

=== "C# / .NET"

    Using [MailKit](https://github.com/jstedfast/MailKit) (recommended):

    ```csharp
    using MailKit.Net.Smtp;
    using MimeKit;

    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse("noreply@myapp.local"));
    message.To.Add(MailboxAddress.Parse("user@example.com"));
    message.Subject = "Test from MyApp";
    message.Body = new TextPart("html") { Text = "<h1>Hello Papercut!</h1>" };

    using var client = new SmtpClient();
    await client.ConnectAsync("localhost", 25, MailKit.Security.SecureSocketOptions.None);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
    ```

    Or classic `System.Net.Mail`:

    ```csharp
    using var client = new SmtpClient("localhost", 25);
    client.Send("noreply@myapp.local", "user@example.com", "Test", "Hello Papercut!");
    ```

=== "ASP.NET Core"

    Typical `appsettings.Development.json` shape (adjust to your email library's config binding):

    ```json
    {
      "Smtp": {
        "Host": "localhost",
        "Port": 25,
        "EnableSsl": false
      }
    }
    ```

    Using **.NET Aspire**? Install `CommunityToolkit.Aspire.Hosting.PapercutSmtp` and add to your App Host:

    ```csharp
    var papercut = builder.AddPapercutSmtp("papercut");

    builder.AddProject<Projects.MyApp>()
        .WithReference(papercut)
        .WaitFor(papercut);
    ```

    Ports are assigned automatically and surfaced in the Aspire dashboard.

=== "Node.js"

    Using [Nodemailer](https://nodemailer.com/):

    ```javascript
    const nodemailer = require("nodemailer");

    const transporter = nodemailer.createTransport({
      host: "localhost",
      port: 25,
      secure: false,
      tls: { rejectUnauthorized: false }
    });

    await transporter.sendMail({
      from: "noreply@myapp.local",
      to: "user@example.com",
      subject: "Test from MyApp",
      html: "<h1>Hello Papercut!</h1>"
    });
    ```

=== "Python"

    Standard library `smtplib`:

    ```python
    import smtplib
    from email.message import EmailMessage

    msg = EmailMessage()
    msg["From"] = "noreply@myapp.local"
    msg["To"] = "user@example.com"
    msg["Subject"] = "Test from MyApp"
    msg.set_content("Hello Papercut!")

    with smtplib.SMTP("localhost", 25) as server:
        server.send_message(msg)
    ```

    Django `settings.py`:

    ```python
    EMAIL_BACKEND = "django.core.mail.backends.smtp.EmailBackend"
    EMAIL_HOST = "localhost"
    EMAIL_PORT = 25
    EMAIL_USE_TLS = False
    ```

=== "PHP"

    [Symfony Mailer](https://symfony.com/doc/current/mailer.html) DSN:

    ```ini
    MAILER_DSN=smtp://localhost:25
    ```

    Laravel `.env`:

    ```ini
    MAIL_MAILER=smtp
    MAIL_HOST=localhost
    MAIL_PORT=25
    MAIL_USERNAME=null
    MAIL_PASSWORD=null
    MAIL_ENCRYPTION=null
    ```

=== "Java"

    Jakarta Mail:

    ```java
    Properties props = new Properties();
    props.put("mail.smtp.host", "localhost");
    props.put("mail.smtp.port", "25");

    Session session = Session.getInstance(props);
    Message message = new MimeMessage(session);
    message.setFrom(new InternetAddress("noreply@myapp.local"));
    message.setRecipients(Message.RecipientType.TO, InternetAddress.parse("user@example.com"));
    message.setSubject("Test from MyApp");
    message.setText("Hello Papercut!");
    Transport.send(message);
    ```

    Spring Boot `application.yml`:

    ```yaml
    spring:
      mail:
        host: localhost
        port: 25
    ```

=== "Ruby"

    Rails `config/environments/development.rb`:

    ```ruby
    config.action_mailer.delivery_method = :smtp
    config.action_mailer.smtp_settings = {
      address: "localhost",
      port: 25
    }
    ```

## Sending from another machine

By default Papercut listens on `127.0.0.1` — localhost only. To catch email from other machines (a VM, a phone, another dev box), open **Options** and set the IP to `Any`, then point the sender at your machine's LAN address. Watch out for firewalls blocking port 25.

## Testing credentials & encrypted connections

Papercut accepts **any** username and password, so leaving your app's real SMTP-auth code path enabled works fine. To exercise STARTTLS/TLS connections, see [TLS & Authentication](smtp-tls-auth.md).
