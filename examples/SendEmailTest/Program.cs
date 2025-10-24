// Papercut SMTP - Send Email Test
// Example console application demonstrating sending multiple test emails
// with embedded images and parallel processing

using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Bogus;
using Microsoft.Extensions.Configuration;
using Papercut.Examples;

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new SmtpSendOptions();
config.GetSection("SmtpSend").Bind(options);

const int EmailCount = 5;

Console.WriteLine("=============================================================");
Console.WriteLine("Papercut SMTP - Bulk Email Test with Embedded Images");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine($"Emails to send: {EmailCount}");
Console.WriteLine("=============================================================\n");

// Load SVG from resources - REQUIRED for this example
var svgPath = Path.Combine(AppContext.BaseDirectory, "resources", "scissors.svg");
if (!File.Exists(svgPath))
{
    Console.WriteLine($"❌ ERROR: SVG file not found at {svgPath}");
    Console.WriteLine("This example requires the SVG file to demonstrate embedded images.");
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
    Environment.Exit(1);
}

var svg = File.ReadAllBytes(svgPath);
Console.WriteLine($"✓ Loaded SVG image from: {svgPath}\n");

try
{
    await SendBulkEmailsAsync(svg);

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ ALL EMAILS SENT SUCCESSFULLY");
    Console.WriteLine("=============================================================");
}
catch (Exception ex)
{
    Console.WriteLine("\n=============================================================");
    Console.WriteLine("❌ ERROR SENDING EMAILS");
    Console.WriteLine("=============================================================");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"\nDetails: {ex}");
    Environment.Exit(1);
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

async Task SendBulkEmailsAsync(byte[] svgData)
{
    var faker = new Faker();
    var tasks = new List<Task>();

    for (int i = 0; i < EmailCount; i++)
    {
        var index = i;
        tasks.Add(Task.Run(() => SendEmailAsync(index, svgData, faker, options)));
    }

    await Task.WhenAll(tasks);
}

void SendEmailAsync(int index, byte[] svgData, Faker faker, SmtpSendOptions smtpOptions)
{
    using var smtpClient = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
    {
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(smtpOptions.Username ?? "username", smtpOptions.Password ?? "password")
    };

    // Generate fake email addresses and names
    var fromName = faker.Name.FullName();
    var fromEmail = faker.Internet.Email();
    var from = new MailAddress(fromEmail, fromName);
    var to = new MailAddress("you@example.com", "Test Recipient");

    using var message = new MailMessage(from, to);

    // Add reply-to
    message.ReplyToList.Add(new MailAddress("reply@example.com"));

    // Set subject with random data
    message.Subject = $"Dear {faker.Name.FullName()} - Fun at {faker.Company.CompanyName()} {Guid.NewGuid()}";
    message.SubjectEncoding = System.Text.Encoding.UTF8;

    // Set priority alternating
    message.Priority = index % 2 == 0 ? MailPriority.Low : MailPriority.Normal;

    // Create HTML body with embedded SVG image
    using var imageStream = new MemoryStream(svgData);

    var linkedResource = new LinkedResource(imageStream, new ContentType("image/svg+xml"))
    {
        ContentId = "image1"
    };

    var htmlBody = $@"<html>
        <head>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px; }}
            .content {{ margin: 20px 0; }}
            .footer {{ color: #666; font-size: 12px; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 10px; }}
            .image-box {{ text-align: center; margin: 20px 0; padding: 20px; background: #f5f5f5; border-radius: 8px; }}
        </style>
        </head>
        <body>
        <div class='header'>
            <h2>Test Email #{index + 1}</h2>
        </div>
        <div class='content'>
            <p>{string.Join("</p><p>", faker.Lorem.Paragraphs(3))}</p>
            <div class='image-box'>
                <p><b>Embedded SVG Image:</b></p>
                <img src=""cid:{linkedResource.ContentId}"" style=""width: 200px"" alt=""Scissors"" />
            </div>
            <p>This is a test email with <b>HTML formatting</b> and an embedded image.</p>
            <p><a href=""http://localhost:5119/weatherforecast"" target=""_blank"">Click here to view weather forecast</a></p>
        </div>
        <div class='footer'>
            <p>Sent: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>Priority: {message.Priority}</p>
        </div>
        </body>
    </html>";

    var alternateView = AlternateView.CreateAlternateViewFromString(
        htmlBody,
        null,
        MediaTypeNames.Text.Html);
    alternateView.LinkedResources.Add(linkedResource);

    message.AlternateViews.Add(alternateView);
    message.IsBodyHtml = true;

    smtpClient.Send(message);

    Console.WriteLine($"✓ Email {index + 1}/{EmailCount} sent: {message.Subject.Substring(0, Math.Min(50, message.Subject.Length))}...");
}
