// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using MailKit.Net.Smtp;

using Microsoft.Extensions.Configuration;

using MimeKit;

using Papercut.Examples;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new SmtpSendOptions();
config.GetSection("SmtpSend").Bind(options);

Console.WriteLine("=============================================================");
Console.WriteLine("Papercut SMTP - TLS/STARTTLS Connection Test");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine($"Authentication: {options.Username}");
Console.WriteLine("=============================================================\n");

try
{
    await TestConnectionAsync();
    await SendTestEmailAsync();

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ ALL TESTS PASSED");
    Console.WriteLine("=============================================================");
}
catch (Exception ex)
{
    Console.WriteLine("\n=============================================================");
    Console.WriteLine("❌ TEST FAILED");
    Console.WriteLine("=============================================================");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"\nDetails: {ex}");
    Environment.Exit(1);
}

async Task TestConnectionAsync()
{
    Console.WriteLine("Test 1: TLS Connection and Authentication");
    Console.WriteLine("-------------------------------------------------------------");

    using var client = new SmtpClient();

    // Accept all certificates (including self-signed) for testing
    client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
    {
        Console.WriteLine($"  Certificate Subject: {certificate?.Subject}");
        Console.WriteLine($"  Certificate Issuer: {certificate?.Issuer}");
        Console.WriteLine($"  SSL Policy Errors: {sslPolicyErrors}");
        return true; // Accept all certificates for testing
    };

    Console.WriteLine($"  Connecting to {options.Host}:{options.Port}...");
    await client.ConnectAsync(options.Host, options.Port, options.Security);
    Console.WriteLine("  ✓ Connection established");

    Console.WriteLine("\n  Server Capabilities:");
    foreach (var capability in client.Capabilities.ToString().Split(','))
    {
        Console.WriteLine($"    - {capability.Trim()}");
    }

    Console.WriteLine($"\n  Authenticating as '{options.Username}'...");
    await client.AuthenticateAsync(options.Username, options.Password);
    Console.WriteLine($"  ✓ Authentication successful (IsAuthenticated: {client.IsAuthenticated})");

    await client.DisconnectAsync(true);
    Console.WriteLine("  ✓ Disconnected successfully\n");
}

async Task SendTestEmailAsync()
{
    Console.WriteLine("Test 2: Send Test Email");
    Console.WriteLine("-------------------------------------------------------------");

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("TLS Test Sender", "sender@example.com"));
    message.To.Add(new MailboxAddress("Test Recipient", "recipient@example.com"));
    message.Subject = $"TLS Test Email - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

    var bodyBuilder = new BodyBuilder
    {
        HtmlBody = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #4CAF50; color: white; padding: 20px; border-radius: 5px; }}
        .info-box {{ background: #f1f1f1; border-left: 4px solid #4CAF50; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🔒 TLS/STARTTLS Test Email</h1>
        <p>Successfully sent via encrypted connection</p>
    </div>
    <div class='info-box'>
        <strong>Server:</strong> {options.Host}:{options.Port}<br />
        <strong>Security:</strong> {options.Security}<br />
        <strong>Authenticated:</strong> {options.Username}<br />
        <strong>Timestamp:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br />
        <strong>Test ID:</strong> {Guid.NewGuid()}
    </div>
    <p>This email was sent using TLS/STARTTLS encryption and SMTP authentication.</p>
    <h3>Features Tested:</h3>
    <ul>
        <li>✓ TLS/STARTTLS Connection</li>
        <li>✓ SMTP Authentication</li>
        <li>✓ HTML Email Body</li>
        <li>✓ Unicode: 你好世界 🌍</li>
    </ul>
</body>
</html>",
        TextBody = $@"
TLS/STARTTLS Test Email
========================

Server: {options.Host}:{options.Port}
Security: {options.Security}
Authenticated: {options.Username}
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

This email was sent using TLS/STARTTLS encryption and SMTP authentication.

Features Tested:
- TLS/STARTTLS Connection
- SMTP Authentication
- Plain Text Email Body
"
    };

    message.Body = bodyBuilder.ToMessageBody();

    using var client = new SmtpClient();
    client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

    Console.WriteLine("  Connecting and sending email...");
    await client.ConnectAsync(options.Host, options.Port, options.Security);
    await client.AuthenticateAsync(options.Username, options.Password);

    var response = await client.SendAsync(message);
    Console.WriteLine($"  ✓ Email sent successfully");
    Console.WriteLine($"  Server response: {response}");

    await client.DisconnectAsync(true);
    Console.WriteLine("  ✓ Disconnected successfully\n");
}
