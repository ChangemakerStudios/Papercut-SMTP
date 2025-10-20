<Query Kind="Program">
  <NuGetReference>MailKit</NuGetReference>
  <NuGetReference>MimeKit</NuGetReference>
  <NuGetReference>RimuTec.Faker</NuGetReference>
  <Namespace>MailKit.Net.Smtp</Namespace>
  <Namespace>MailKit.Security</Namespace>
  <Namespace>MimeKit</Namespace>
  <Namespace>RimuTec.Faker</Namespace>
  <Namespace>System.Net.Security</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

// ============================================================================
// Papercut SMTP - TLS/STARTTLS Test Script
// ============================================================================
// This script tests SMTP authentication and TLS/STARTTLS connections
// to Papercut SMTP server (issue #102)
//
// Requirements:
// - Papercut Service running with TLS configured
// - Certificate installed in Windows certificate store
//
// Configuration Options:
// - SMTP_HOST: Server address (default: localhost)
// - SMTP_PORT: SMTP port (587 for STARTTLS, 465 for TLS, 25 for plain)
// - USE_TLS: Connection security mode
// ============================================================================

// Configuration
const string SMTP_HOST = "localhost";
const int SMTP_PORT = 587;  // Change to 25 for plain, 587 for STARTTLS, 465 for TLS
const SecureSocketOptions USE_TLS = SecureSocketOptions.StartTls;  // Options: None, StartTls, SslOnConnect

// Authentication (Papercut accepts any credentials by default)
const string USERNAME = "testuser";
const string PASSWORD = "testpass";

void Main()
{
	Console.WriteLine("=============================================================");
	Console.WriteLine("Papercut SMTP - TLS/STARTTLS Connection Test");
	Console.WriteLine("=============================================================");
	Console.WriteLine($"Server: {SMTP_HOST}:{SMTP_PORT}");
	Console.WriteLine($"Security: {USE_TLS}");
	Console.WriteLine($"Authentication: {USERNAME} / {PASSWORD}");
	Console.WriteLine("=============================================================\n");

	try
	{
		// Test 1: Basic TLS Connection
		TestBasicTLSConnection();

		// Test 2: Authentication Test
		TestAuthentication();

		// Test 3: Send Test Email
		SendTestEmail();

		// Test 4: Send Multiple Emails (stress test)
		SendMultipleEmails(3);

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
		Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");

		if (ex.InnerException != null)
		{
			Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
		}
	}
}

void TestBasicTLSConnection()
{
	Console.WriteLine("Test 1: Basic TLS Connection");
	Console.WriteLine("-------------------------------------------------------------");

	using var client = new SmtpClient();

	// Optional: Accept all certificates (for testing with self-signed certs)
	client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
	{
		Console.WriteLine($"  Certificate Subject: {certificate?.Subject}");
		Console.WriteLine($"  Certificate Issuer: {certificate?.Issuer}");
		Console.WriteLine($"  Valid From: {certificate?.GetEffectiveDateString()}");
		Console.WriteLine($"  Valid To: {certificate?.GetExpirationDateString()}");
		Console.WriteLine($"  SSL Policy Errors: {sslPolicyErrors}");

		// Accept all certificates (including self-signed) for testing
		// In production, you should validate the certificate properly
		return true;
	};

	try
	{
		Console.WriteLine($"  Connecting to {SMTP_HOST}:{SMTP_PORT}...");
		client.Connect(SMTP_HOST, SMTP_PORT, USE_TLS);
		Console.WriteLine("  ✓ Connection established");

		// Display server capabilities
		Console.WriteLine("\n  Server Capabilities:");
		foreach (var capability in client.Capabilities.ToString().Split(','))
		{
			Console.WriteLine($"    - {capability.Trim()}");
		}

		client.Disconnect(true);
		Console.WriteLine("  ✓ Disconnected successfully\n");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ Connection failed: {ex.Message}\n");
		throw;
	}
}

void TestAuthentication()
{
	Console.WriteLine("Test 2: SMTP Authentication");
	Console.WriteLine("-------------------------------------------------------------");

	using var client = new SmtpClient();
	client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

	try
	{
		Console.WriteLine($"  Connecting to {SMTP_HOST}:{SMTP_PORT}...");
		client.Connect(SMTP_HOST, SMTP_PORT, USE_TLS);

		Console.WriteLine($"  Authenticating as '{USERNAME}'...");
		client.Authenticate(USERNAME, PASSWORD);
		Console.WriteLine("  ✓ Authentication successful");

		// Check if authentication was actually required
		if (client.IsAuthenticated)
		{
			Console.WriteLine("  ✓ Client is authenticated");
		}
		else
		{
			Console.WriteLine("  ⚠ Client is connected but not authenticated (server may not require auth)");
		}

		client.Disconnect(true);
		Console.WriteLine("  ✓ Disconnected successfully\n");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ Authentication failed: {ex.Message}\n");
		throw;
	}
}

void SendTestEmail()
{
	Console.WriteLine("Test 3: Send Test Email with TLS");
	Console.WriteLine("-------------------------------------------------------------");

	var message = new MimeMessage();
	message.From.Add(new MailboxAddress(Name.NameWithMiddle(), Name.FirstName() + "@example.com"));
	message.To.Add(new MailboxAddress("Test Recipient", "recipient@example.com"));
	message.Subject = $"TLS Test Email - {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {Guid.NewGuid()}";

	var bodyBuilder = new BodyBuilder();
	bodyBuilder.HtmlBody = $@"
<html>
<head>
	<style>
		body {{ font-family: Arial, sans-serif; margin: 20px; }}
		.header {{ background: #4CAF50; color: white; padding: 20px; border-radius: 5px; }}
		.content {{ margin-top: 20px; line-height: 1.6; }}
		.info-box {{ background: #f1f1f1; border-left: 4px solid #4CAF50; padding: 15px; margin: 20px 0; }}
		.footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
	</style>
</head>
<body>
	<div class='header'>
		<h1>🔒 TLS/STARTTLS Connection Test</h1>
		<p>Testing SMTP Authentication and Encryption</p>
	</div>

	<div class='content'>
		<h2>Connection Details</h2>
		<div class='info-box'>
			<strong>Server:</strong> {SMTP_HOST}:{SMTP_PORT}<br />
			<strong>Security:</strong> {USE_TLS}<br />
			<strong>Authenticated:</strong> {USERNAME}<br />
			<strong>Timestamp:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br />
			<strong>Test ID:</strong> {Guid.NewGuid()}
		</div>

		<h2>Test Content</h2>
		<p>{string.Join("</p><p>", Lorem.Paragraphs())}</p>

		<h2>Features Tested</h2>
		<ul>
			<li>✓ TLS/STARTTLS Connection</li>
			<li>✓ SMTP Authentication</li>
			<li>✓ HTML Email Body</li>
			<li>✓ Unicode Characters: 你好世界 🌍 Привет мир</li>
			<li>✓ Special Characters: &lt;&gt;&amp;&quot;'</li>
		</ul>
	</div>

	<div class='footer'>
		<p><strong>Papercut SMTP - Issue #102</strong></p>
		<p>TLS/STARTTLS and SMTP Authentication Support</p>
		<p>Generated by LINQPad test script</p>
	</div>
</body>
</html>";

	bodyBuilder.TextBody = $@"
TLS/STARTTLS Connection Test
=============================

Connection Details:
- Server: {SMTP_HOST}:{SMTP_PORT}
- Security: {USE_TLS}
- Authenticated: {USERNAME}
- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Test Content:
{string.Join("\n\n", Lorem.Paragraphs())}

Features Tested:
- TLS/STARTTLS Connection
- SMTP Authentication
- Plain Text Email Body

Papercut SMTP - Issue #102
";

	message.Body = bodyBuilder.ToMessageBody();

	using var client = new SmtpClient();
	client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

	try
	{
		Console.WriteLine("  Connecting and sending email...");
		client.Connect(SMTP_HOST, SMTP_PORT, USE_TLS);
		client.Authenticate(USERNAME, PASSWORD);

		var response = client.Send(message);
		Console.WriteLine($"  ✓ Email sent successfully");
		Console.WriteLine($"  Server response: {response}");

		client.Disconnect(true);
		Console.WriteLine("  ✓ Disconnected successfully\n");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ Send failed: {ex.Message}\n");
		throw;
	}
}

void SendMultipleEmails(int count)
{
	Console.WriteLine($"Test 4: Send Multiple Emails (n={count})");
	Console.WriteLine("-------------------------------------------------------------");

	using var client = new SmtpClient();
	client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

	try
	{
		Console.WriteLine("  Connecting...");
		client.Connect(SMTP_HOST, SMTP_PORT, USE_TLS);
		client.Authenticate(USERNAME, PASSWORD);
		Console.WriteLine("  ✓ Connected and authenticated\n");

		for (int i = 1; i <= count; i++)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(Name.NameWithMiddle(), Name.FirstName() + "@example.com"));
			message.To.Add(new MailboxAddress("Bulk Test Recipient", "bulk-test@example.com"));
			message.Subject = $"Bulk Test Email {i}/{count} - {Guid.NewGuid()}";
			message.Body = new TextPart("plain")
			{
				Text = $"This is bulk test email {i} of {count}.\n\n{string.Join("\n\n", Lorem.Paragraphs())}"
			};

			Console.Write($"  Sending email {i}/{count}... ");
			client.Send(message);
			Console.WriteLine("✓ Sent");
		}

		Console.WriteLine($"\n  ✓ All {count} emails sent successfully");
		client.Disconnect(true);
		Console.WriteLine("  ✓ Disconnected successfully\n");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"\n  ✗ Bulk send failed: {ex.Message}\n");
		throw;
	}
}

// Additional helper method for troubleshooting
void DiagnoseConnection()
{
	Console.WriteLine("\nConnection Diagnostics");
	Console.WriteLine("=============================================================");

	using var client = new SmtpClient();
	client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

	// Try to connect with minimal options
	try
	{
		Console.WriteLine("Testing connection with no security...");
		client.Connect(SMTP_HOST, SMTP_PORT, SecureSocketOptions.None);
		Console.WriteLine("  ✓ Plain connection works");
		client.Disconnect(true);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ Plain connection failed: {ex.Message}");
	}

	// Try STARTTLS
	try
	{
		Console.WriteLine("\nTesting connection with STARTTLS...");
		client.Connect(SMTP_HOST, SMTP_PORT, SecureSocketOptions.StartTls);
		Console.WriteLine("  ✓ STARTTLS connection works");
		Console.WriteLine($"  Capabilities: {client.Capabilities}");
		client.Disconnect(true);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ STARTTLS connection failed: {ex.Message}");
	}

	// Try immediate TLS
	try
	{
		Console.WriteLine("\nTesting connection with immediate TLS...");
		client.Connect(SMTP_HOST, 465, SecureSocketOptions.SslOnConnect);
		Console.WriteLine("  ✓ Immediate TLS connection works");
		client.Disconnect(true);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  ✗ Immediate TLS connection failed: {ex.Message}");
	}
}

// Uncomment to run diagnostics:
// DiagnoseConnection();
