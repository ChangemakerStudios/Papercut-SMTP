// Papercut SMTP - Email with file:/// Links Test
// Example console application demonstrating file:/// protocol links (Issue #232)
// Tests that file links open correctly in Windows Explorer or associated applications

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Papercut.Examples;

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new SmtpSendOptions();
config.GetSection("SmtpSend").Bind(options);

Console.WriteLine("=============================================================");
Console.WriteLine("Papercut SMTP - Email with file:/// Links Test (Issue #232)");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine("=============================================================\n");

try
{
    await SendFileLinkEmailAsync();

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ EMAIL WITH FILE LINKS SENT SUCCESSFULLY");
    Console.WriteLine("=============================================================");
    Console.WriteLine("\nOpen this email in Papercut and click the file:/// links");
    Console.WriteLine("to test that they open correctly in Windows Explorer or");
    Console.WriteLine("their associated applications.");
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

async Task SendFileLinkEmailAsync()
{
    using var smtpClient = new SmtpClient(options.Host, options.Port)
    {
        UseDefaultCredentials = false,
        Credentials = !string.IsNullOrEmpty(options.Username)
            ? new NetworkCredential(options.Username, options.Password ?? string.Empty)
            : null,
        EnableSsl = options.Security != SmtpSecurityMode.None
    };

    // Configure SSL/TLS based on security mode
    if (options.Security == SmtpSecurityMode.SslOnConnect)
    {
        // Port 465 typically uses implicit TLS
        // Additional TLS configuration if needed
    }

    var from = new MailAddress("test@company.com", "Test Sender");
    var to = new MailAddress("user@example.com", "Test User");

    using var message = new MailMessage(from, to);

    message.Subject = "Test Email with file:/// Links (Issue #232)";
    message.SubjectEncoding = System.Text.Encoding.UTF8;

    // HTML email with file:/// links
    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/");

    message.Body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>File Links Test</title>
    <style>
        body {{
            font-family: Arial, Helvetica, sans-serif;
            margin: 20px;
            line-height: 1.6;
        }}
        h1 {{
            color: #333;
        }}
        .test-section {{
            margin: 20px 0;
            padding: 15px;
            border-left: 4px solid #667eea;
            background-color: #f8f9fa;
        }}
        a {{
            color: #667eea;
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        code {{
            background-color: #e9ecef;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }}
    </style>
</head>
<body>
    <h1>🔗 Testing file:/// Links</h1>

    <p>This email tests the functionality of <code>file:///</code> links in Papercut SMTP (Issue #232).</p>

    <div class=""test-section"">
        <h2>Test 1: Link to a Directory</h2>
        <p>Click this link to open your Windows directory in Explorer:</p>
        <p><a href=""file:///C:/Windows"">📁 Open C:\Windows Directory</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 2: Link to a File</h2>
        <p>Click this link to open notepad.exe with its associated application:</p>
        <p><a href=""file:///C:/Windows/System32/notepad.exe"">📄 Open Notepad</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 3: Link to a Text File</h2>
        <p>Click this link to open a common Windows text file:</p>
        <p><a href=""file:///C:/Windows/system.ini"">📝 Open system.ini</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 4: Link to User Profile Directory</h2>
        <p>Click this link to open your user profile directory:</p>
        <p><a href=""file:///{userProfile}"">👤 Open User Profile</a></p>
    </div>

    <div class=""test-section"">
        <h2>Expected Behavior</h2>
        <ul>
            <li>✅ Directory links should open in Windows Explorer</li>
            <li>✅ File links should open with their associated application</li>
            <li>❌ Links should NOT be treated as downloads in the WebView2 browser</li>
        </ul>
    </div>

    <hr>
    <p style=""color: #666; font-size: 14px;"">
        This test email was generated for
        <a href=""https://github.com/ChangemakerStudios/Papercut-SMTP/issues/232"">Issue #232</a> -
        Make file links functional
    </p>
</body>
</html>";

    message.BodyEncoding = System.Text.Encoding.UTF8;
    message.IsBodyHtml = true;
    message.Priority = MailPriority.Normal;

    await smtpClient.SendMailAsync(message);

    Console.WriteLine("✓ Test email sent successfully!");
    Console.WriteLine($"  Subject: {message.Subject}");
    Console.WriteLine($"  To: {to.DisplayName} <{to.Address}>");
    Console.WriteLine($"  From: {from.DisplayName} <{from.Address}>");
}
