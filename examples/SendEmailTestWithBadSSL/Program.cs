// Papercut SMTP - SSL Certificate Error Test
// Example console application demonstrating how Papercut handles HTML emails
// with images from sites with various SSL certificate issues (badssl.com)

using Bogus;
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
Console.WriteLine("Papercut SMTP - SSL Certificate Error Test");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine("=============================================================\n");
Console.WriteLine("This test sends an email with images from badssl.com");
Console.WriteLine("to verify SSL certificate error handling in Papercut.");
Console.WriteLine();

try
{
    await SendBadSSLTestEmailAsync();

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ SSL TEST EMAIL SENT SUCCESSFULLY");
    Console.WriteLine("=============================================================");
    Console.WriteLine("\nOpen this email in Papercut to check SSL certificate handling:");
    Console.WriteLine("  • With 'Ignore SSL Errors' enabled: All 5 images should display");
    Console.WriteLine("  • With 'Ignore SSL Errors' disabled: Only the good SSL image");
    Console.WriteLine("    should display");
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

async Task SendBadSSLTestEmailAsync()
{
    var faker = new Faker();

    using var smtpClient = await SmtpClientHelper.CreateAndConnectAsync(options);

    var fromName = faker.Name.FullName();
    var fromEmail = faker.Internet.Email();

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(fromName, fromEmail));
    message.To.Add(new MailboxAddress("Test Recipient", "you@example.com"));
    message.ReplyTo.Add(new MailboxAddress("Reply", "reply@example.com"));

    message.Subject = "SSL Certificate Test - " + Guid.NewGuid();
    message.Priority = MessagePriority.Normal;

    // HTML body with images from various bad SSL scenarios
    var bodyBuilder = new BodyBuilder
    {
        HtmlBody = $@"<html>
<body>
	<h2>SSL Certificate Error Test Email</h2>
	<p>This email contains PNG images from various bad SSL certificate scenarios from badssl.com</p>

	<h3>1. Expired Certificate</h3>
	<img src=""https://expired.badssl.com/icons/icon-red.png"" alt=""Expired"" style=""width: 100px; border: 2px solid red;"" />

	<h3>2. Wrong Host Certificate</h3>
	<img src=""https://wrong.host.badssl.com/icons/icon-orange.png"" alt=""Wrong Host"" style=""width: 100px; border: 2px solid orange;"" />

	<h3>3. Self-Signed Certificate</h3>
	<img src=""https://self-signed.badssl.com/icons/icon-yellow.png"" alt=""Self Signed"" style=""width: 100px; border: 2px solid yellow;"" />

	<h3>4. Untrusted Root Certificate</h3>
	<img src=""https://untrusted-root.badssl.com/icons/icon-red.png"" alt=""Untrusted Root"" style=""width: 100px; border: 2px solid purple;"" />

	<h3>5. Good SSL (for comparison)</h3>
	<img src=""https://badssl.com/icons/icon-green.png"" alt=""Good SSL"" style=""width: 100px; border: 2px solid green;"" />

	<hr />
	<p><b>Expected behavior:</b></p>
	<ul>
		<li>With <i>Ignore SSL Certificate Errors</i> <b>enabled</b>: All 5 images should display</li>
		<li>With <i>Ignore SSL Certificate Errors</i> <b>disabled</b>: Only the last image (Good SSL) should display</li>
	</ul>

	<p>{faker.Lorem.Paragraphs(3, "<br />")}</p>
</body>
</html>"
    };

    message.Body = bodyBuilder.ToMessageBody();

    await smtpClient.SendAsync(message);
    await smtpClient.DisconnectAsync(true);

    Console.WriteLine("✓ Test email sent successfully!");
    Console.WriteLine($"  Subject: {message.Subject}");
    Console.WriteLine($"  To: {message.To}");
    Console.WriteLine($"  From: {message.From}");
}
