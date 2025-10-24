// Papercut SMTP - Email with PDF Attachment Test
// Example console application demonstrating sending professional invoice emails
// with PDF attachments

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

Console.WriteLine("=============================================================");
Console.WriteLine("Papercut SMTP - Email with PDF Attachment Test");
Console.WriteLine("=============================================================");
Console.WriteLine($"Server: {options.Host}:{options.Port}");
Console.WriteLine($"Security: {options.Security}");
Console.WriteLine("=============================================================\n");

try
{
    await SendInvoiceEmailAsync();

    Console.WriteLine("\n=============================================================");
    Console.WriteLine("✅ INVOICE EMAIL SENT SUCCESSFULLY");
    Console.WriteLine("=============================================================");
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

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

async Task SendInvoiceEmailAsync()
{
    var faker = new Faker();
    var random = new Random();
    var companyName = faker.Company.CompanyName();
    var customerFullName = faker.Name.FullName();
    var invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMM") + "-" + random.Next(1000, 9999);
    var amount = random.Next(500, 5000);

    using var smtpClient = new SmtpClient(options.Host, options.Port)
    {
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(options.Username ?? "username", options.Password ?? "password")
    };

    var from = new MailAddress("invoices@acmecorp.com", $"{companyName} Billing");
    var to = new MailAddress("customer@example.com", customerFullName);

    using var message = new MailMessage(from, to);

    message.ReplyToList.Add(new MailAddress("billing@acmecorp.com"));
    message.Subject = $"Your Invoice {invoiceNumber} - Payment Confirmation";
    message.SubjectEncoding = System.Text.Encoding.UTF8;

    // Professional invoice-style HTML email template
    message.Body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invoice Email</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #e8eef3;
        }}
        .email-container {{
            max-width: 650px;
            margin: 30px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 8px 20px rgba(0,0,0,0.15);
        }}
        .header {{
            background: linear-gradient(90deg, #1e3c72 0%, #2a5298 100%);
            padding: 30px 40px;
            color: white;
            border-bottom: 4px solid #f39c12;
        }}
        .header h1 {{
            margin: 0 0 8px 0;
            font-size: 32px;
            font-weight: 600;
            letter-spacing: -0.5px;
        }}
        .header p {{
            margin: 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .invoice-banner {{
            background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%);
            padding: 20px 40px;
            color: white;
            text-align: center;
        }}
        .invoice-banner h2 {{
            margin: 0;
            font-size: 22px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 40px 30px 40px;
        }}
        .content p {{
            color: #444444;
            font-size: 16px;
            line-height: 1.7;
            margin: 0 0 15px 0;
        }}
        .info-box {{
            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
            border-left: 4px solid #1e3c72;
            padding: 25px;
            margin: 25px 0;
            border-radius: 6px;
        }}
        .info-box h3 {{
            margin: 0 0 15px 0;
            color: #1e3c72;
            font-size: 18px;
            font-weight: 600;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #dee2e6;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            color: #666666;
            font-weight: 500;
        }}
        .info-value {{
            color: #333333;
            font-weight: 600;
        }}
        .attachment-notice {{
            background-color: #fff3cd;
            border: 2px dashed #ffc107;
            padding: 20px;
            margin: 25px 0;
            border-radius: 8px;
            text-align: center;
        }}
        .attachment-notice .icon {{
            font-size: 32px;
            margin-bottom: 10px;
        }}
        .attachment-notice p {{
            margin: 5px 0;
            color: #856404;
            font-weight: 500;
        }}
        .button {{
            display: inline-block;
            padding: 16px 40px;
            margin: 25px 0;
            background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0 4px 12px rgba(30, 60, 114, 0.3);
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px 40px;
            text-align: center;
            border-top: 1px solid #dee2e6;
        }}
        .footer p {{
            color: #6c757d;
            font-size: 13px;
            line-height: 1.6;
            margin: 8px 0;
        }}
        .footer a {{
            color: #1e3c72;
            text-decoration: none;
            font-weight: 500;
        }}
        .divider {{
            height: 2px;
            background: linear-gradient(90deg, transparent, #dee2e6, transparent);
            margin: 30px 0;
        }}
        .highlight {{
            background-color: #fff3cd;
            padding: 2px 6px;
            border-radius: 3px;
            font-weight: 600;
            color: #856404;
        }}
        @media only screen and (max-width: 650px) {{
            .email-container {{
                margin: 10px;
            }}
            .content, .header, .invoice-banner, .footer {{
                padding: 20px;
            }}
            .info-row {{
                flex-direction: column;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <!-- Header -->
        <div class=""header"">
            <h1>{companyName}</h1>
            <p>Professional Services & Solutions</p>
        </div>

        <!-- Invoice Banner -->
        <div class=""invoice-banner"">
            <h2>📄 Invoice & Payment Confirmation</h2>
        </div>

        <!-- Main Content -->
        <div class=""content"">
            <p>Dear {faker.Name.FirstName()},</p>

            <p>Thank you for your business! This email confirms that we have received your payment and your invoice has been processed successfully.</p>

            <div class=""info-box"">
                <h3>📋 Transaction Details</h3>
                <div class=""info-row"">
                    <span class=""info-label"">Invoice Number:</span>
                    <span class=""info-value"">{invoiceNumber}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Issue Date:</span>
                    <span class=""info-value"">{DateTime.Now:MMMM dd, yyyy}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Payment Method:</span>
                    <span class=""info-value"">Credit Card (****{random.Next(1000, 9999)})</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Amount Paid:</span>
                    <span class=""info-value"">${amount:N2} USD</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Status:</span>
                    <span class=""info-value"" style=""color: #28a745;"">✓ PAID</span>
                </div>
            </div>

            <div class=""attachment-notice"">
                <div class=""icon"">📎</div>
                <p><strong>PDF Invoice Attached</strong></p>
                <p style=""font-size: 14px;"">Your detailed invoice is attached to this email as a PDF document.</p>
            </div>

            <p>The attached PDF contains a complete breakdown of services, itemized charges, and payment information for your records.</p>

            <div class=""divider""></div>

            <p><strong>What's Next?</strong></p>
            <ul style=""color: #444444; line-height: 1.8;"">
                <li>Save the attached PDF for your tax records</li>
                <li>Access your invoice history anytime in your account portal</li>
                <li>Set up auto-pay to never miss a payment</li>
                <li>Contact us if you have any questions about this invoice</li>
            </ul>

            <center>
                <a href=""https://localhost:5000/invoices/{invoiceNumber}"" class=""button"">View Invoice Online</a>
            </center>

            <div class=""divider""></div>

            <p>Thank you for choosing <span class=""highlight"">{companyName}</span>. We appreciate your continued partnership!</p>

            <p>Best regards,<br>
            <strong>Accounts Receivable Team</strong><br>
            {companyName}</p>
        </div>

        <!-- Footer -->
        <div class=""footer"">
            <p><strong>{companyName}</strong><br>
            {faker.Address.StreetAddress()}, {faker.Address.City()}, {faker.Address.StateAbbr()} {faker.Address.ZipCode()}<br>
            Phone: {faker.Phone.PhoneNumber()} | Email: billing@acmecorp.com</p>

            <p style=""margin-top: 20px;"">
                <a href=""#"">Payment Portal</a> |
                <a href=""#"">Billing FAQ</a> |
                <a href=""#"">Contact Support</a>
            </p>

            <p style=""font-size: 12px; color: #adb5bd; margin-top: 20px;"">
                This is an automated message. Please do not reply directly to this email.<br>
                For billing inquiries, please contact billing@acmecorp.com<br>
                &copy; {DateTime.Now.Year} {companyName}. All rights reserved.
            </p>
        </div>
    </div>
</body>
</html>";

    message.BodyEncoding = System.Text.Encoding.UTF8;
    message.IsBodyHtml = true;
    message.Priority = MailPriority.High;

    // Attach the PDF file if it exists
    var pdfPath = Path.Combine(AppContext.BaseDirectory, "resources", "sample.pdf");

    if (File.Exists(pdfPath))
    {
        var pdfAttachment = new Attachment(pdfPath, MediaTypeNames.Application.Pdf)
        {
            Name = $"Invoice-{invoiceNumber}.pdf"
        };
        message.Attachments.Add(pdfAttachment);
        Console.WriteLine($"✓ PDF attachment added: {pdfPath}");
    }
    else
    {
        Console.WriteLine($"⚠ Warning: PDF file not found at {pdfPath}");
        Console.WriteLine("  Email will be sent without attachment");
        Console.WriteLine("  To include a PDF, place a 'sample.pdf' file in the same directory as the executable\n");
    }

    await Task.Run(() => smtpClient.Send(message));

    Console.WriteLine();
    Console.WriteLine("✓ Email sent successfully!");
    Console.WriteLine($"  Subject: {message.Subject}");
    Console.WriteLine($"  To: {to.DisplayName} <{to.Address}>");
    Console.WriteLine($"  From: {from.DisplayName} <{from.Address}>");
    Console.WriteLine($"  Attachments: {message.Attachments.Count}");
}
