// Papercut SMTP - CSS Word-Wrap Test Email (Issue #154)
// Example console application demonstrating the CSS word-wrap fix
// Sends an HTML email with various long text strings to test word-wrapping behavior

using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Papercut.Examples;

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new SmtpSendOptions();
config.GetSection("SmtpSend").Bind(options);

Console.WriteLine("=============================================================");
Console.WriteLine("Papercut SMTP - CSS Word-Wrap Test (Issue #154)");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine("=============================================================\n");

try
{
    await SendWordWrapTestEmailAsync();

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ WORD-WRAP TEST EMAIL SENT SUCCESSFULLY");
    Console.WriteLine("=============================================================");
    Console.WriteLine("\nCheck Papercut to verify:");
    Console.WriteLine("  • Long text strings wrap properly without horizontal scrolling");
    Console.WriteLine("  • CSS word-wrap properties are honored on all elements");
    Console.WriteLine("  • Text breaks at appropriate boundaries in <p>, <code>, <pre> tags");
}
catch (Exception ex)
{
    Console.WriteLine("\n=============================================================");
    Console.WriteLine("❌ ERROR SENDING EMAIL");
    Console.WriteLine("=============================================================");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"\nDetails: {ex}");
    Environment.Exit(1);
}

async Task SendWordWrapTestEmailAsync()
{
    // Read the test HTML file from resources directory
    var htmlFilePath = Path.Combine(AppContext.BaseDirectory, "resources", "test-word-wrap.html");

    if (!File.Exists(htmlFilePath))
    {
        throw new FileNotFoundException($"Test HTML file not found at: {htmlFilePath}");
    }

    var htmlContent = await File.ReadAllTextAsync(htmlFilePath);

    using var smtpClient = await SmtpClientHelper.CreateAndConnectAsync(options);

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("CSS Word-Wrap Test", "wordwrap-test@papercut.local"));
    message.To.Add(new MailboxAddress("Test Recipient", "recipient@example.com"));
    message.Subject = "CSS Word-Wrap Test - Issue #154";

    var bodyBuilder = new BodyBuilder
    {
        HtmlBody = htmlContent
    };

    message.Body = bodyBuilder.ToMessageBody();
    message.Priority = MessagePriority.Normal;

    await smtpClient.SendAsync(message);
    await smtpClient.DisconnectAsync(true);

    Console.WriteLine("✓ Word-wrap test email sent successfully!");
    Console.WriteLine($"  Subject: {message.Subject}");
    Console.WriteLine($"  To: {message.To}");
    Console.WriteLine($"  From: {message.From}");
    Console.WriteLine($"  HTML File: {htmlFilePath}");
}
