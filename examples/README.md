# Papercut SMTP - Example Applications

This directory contains example console applications that demonstrate how to send test emails to Papercut SMTP. These examples are useful for testing Papercut's email viewing and handling capabilities.

## Available Examples

### [SendEmailTest](SendEmailTest/)
Sends 5 test emails in parallel with embedded SVG images and randomly generated content.

**Features:**
- Parallel email sending (demonstrates concurrency)
- Embedded SVG images using LinkedResource
- Fake data generation with Bogus
- Alternating email priorities (Low/Normal)
- HTML formatted emails with styling

**Requirements:** `scissors.svg` file (included in resources)

**Usage:**
```powershell
cd examples/SendEmailTest
dotnet run
```

---

### [SendRichEmailTest](SendRichEmailTest/)
Sends a professional HTML email with modern styling and responsive design.

**Features:**
- Professional welcome email template
- Modern CSS with gradients and shadows
- Responsive design for mobile/desktop
- Call-to-action buttons
- Social media links and footer

**Usage:**
```powershell
cd examples/SendRichEmailTest
dotnet run
```

---

### [SendEmailWithPdfAttachment](SendEmailWithPdfAttachment/)
Sends an invoice-style email with a PDF attachment.

**Features:**
- Professional invoice email template
- PDF file attachment
- Transaction details and payment information
- Modern styling with gradients

**Requirements:** `sample.pdf` file (included in resources)

**Usage:**
```powershell
cd examples/SendEmailWithPdfAttachment
dotnet run
```

---

### [SendEmailWithFileLinks](SendEmailWithFileLinks/)
Tests `file:///` protocol links in HTML emails (Issue #232).

**Features:**
- Links to local directories (Windows Explorer)
- Links to local files with associated applications
- Tests Papercut's handling of file:// URLs
- Validates WebView2 integration

**Purpose:** Verify that file:// links open correctly in Windows Explorer or associated applications rather than triggering downloads.

**Usage:**
```powershell
cd examples/SendEmailWithFileLinks
dotnet run
```

---

### [SendEmailTestWithBadSSL](SendEmailTestWithBadSSL/)
Tests SSL certificate error handling by embedding images from badssl.com.

**Features:**
- Images from expired certificates
- Images from wrong host certificates
- Images from self-signed certificates
- Images from untrusted root certificates
- Comparison with valid SSL (good) certificate

**Purpose:** Test Papercut's "Ignore SSL Certificate Errors" setting to ensure it correctly handles various SSL/TLS certificate validation scenarios.

**Usage:**
```powershell
cd examples/SendEmailTestWithBadSSL
dotnet run
```

---

### [SendEmailWithTls](SendEmailWithTls/)
Demonstrates SMTP authentication and TLS/STARTTLS connections (Issue #102).

**Features:**
- TLS/STARTTLS connection testing
- SMTP authentication
- Server certificate validation callback
- Server capability detection
- Both HTML and plain text email bodies
- Comprehensive connection diagnostics

**Configuration:** Modify constants in `Program.cs` to match your setup:
- `SmtpHost` - SMTP server address (default: localhost)
- `SmtpPort` - SMTP port (25=plain, 587=STARTTLS, 465=TLS)
- `Security` - SecureSocketOptions (None, StartTls, SslOnConnect)
- `Username/Password` - Authentication credentials

**Usage:**
```powershell
cd examples/SendEmailWithTls
dotnet run
```

---

## Prerequisites

All examples require:
- **.NET 8.0 Runtime** or later
- **Papercut SMTP** running on `127.0.0.1:25` (or modify the `SmtpHost`/`SmtpPort` constants)

## Shared Resources

The [resources/](resources/) directory contains shared files used by multiple examples:
- `scissors.svg` - SVG image for embedded image testing
- `sample.pdf` - PDF document for attachment testing

**Note:** All resources are automatically copied to the `resources/` subdirectory in each project's build output via [Directory.Build.props](Directory.Build.props). Access them in code using:
```csharp
var path = Path.Combine(AppContext.BaseDirectory, "resources", "filename.ext");
```

## Centralized Configuration

### SMTP Settings ([appsettings.json](appsettings.json))

All examples (except SendEmailWithTls) load SMTP configuration from a shared `appsettings.json` file:

```json
{
  "SmtpSend": {
    "Host": "127.0.0.1",
    "Port": 25,
    "Security": "None",
    "Username": null,
    "Password": null
  }
}
```

**Security options:**
- `None` - Plain SMTP (port 25)
- `StartTls` - STARTTLS upgrade (port 587)
- `SslOnConnect` - Immediate TLS (port 465)

**To configure all examples at once**, edit `examples/appsettings.json` - changes apply to all projects automatically!

### Package Management

All examples use a centralized [Directory.Build.props](Directory.Build.props) file that:
- Sets common project properties (OutputType, TargetFramework, etc.)
- Manages NuGet package versions in one place
- Conditionally includes packages based on project needs
- Shares the [SmtpSendOptions.cs](SmtpSendOptions.cs) configuration class

Each project file simply sets flags to enable features:
```xml
<PropertyGroup>
  <UseBogus>true</UseBogus>            <!-- Include Bogus for fake data -->
  <UseMailKit>true</UseMailKit>        <!-- Include MailKit/MimeKit -->
  <UseConfiguration>true</UseConfiguration>  <!-- Include configuration support -->
</PropertyGroup>
```

This approach ensures:
- ✅ Consistent package versions across all examples
- ✅ Simplified project files
- ✅ Easy version updates in one location
- ✅ No package duplication
- ✅ Single configuration file for all SMTP settings

## Building All Examples

To build all example projects at once:

```powershell
dotnet build Papercut.sln
```

Or build individual examples:

```powershell
dotnet build examples/SendEmailTest/SendEmailTest.csproj
```

## Common NuGet Packages

Most examples use these packages:
- **Bogus** - Fake data generation (names, addresses, lorem ipsum)
- **MailKit** - Modern SMTP client (used by SendEmailWithTls)
- **MimeKit** - MIME message construction (used by SendEmailWithTls)

The simpler examples use the built-in `System.Net.Mail.SmtpClient` class.

## Modifying Examples

Each example is self-contained with configuration constants at the top of `Program.cs`:

```csharp
const string SmtpHost = "127.0.0.1";
const int SmtpPort = 25;
```

Feel free to modify these values to test different SMTP servers or scenarios.

## Contributing

When adding new examples:
1. Follow the `SendEmail*` naming convention
2. Include descriptive comments in `Program.cs`
3. Add proper error handling
4. Update this README with a new section
5. Keep examples focused on a single feature or scenario
6. Use shared resources from the `resources/` folder when possible
7. Use property flags (`<UseBogus>`, `<UseMailKit>`) instead of explicit package references
8. Don't duplicate common properties - they're in `Directory.Build.props`

## Troubleshooting

**"Connection refused"** - Ensure Papercut SMTP is running and listening on the correct port.

**"SVG/PDF file not found"** - The resources should be automatically copied to the build output directory. Check the `.csproj` file for `<ItemGroup>` entries.

**"Permission denied"** - Run Papercut SMTP with appropriate permissions, or modify the examples to use a non-privileged port.

## Related

See also:
- [../linqpad/](../linqpad/) - Original LINQPad scripts these examples were converted from
- [../src/](../src/) - Papercut SMTP source code
- [Issue #102](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/102) - TLS/STARTTLS support
- [Issue #232](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/232) - File:// link support
