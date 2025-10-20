# SMTP Authentication and TLS/STARTTLS Implementation

## Overview

This document describes the implementation of SMTP authentication and TLS/STARTTLS support in Papercut SMTP for issue #102.

**Issue**: [#102 - Support for SMTP Auth and TLS](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/102)

**Branch**: `feature/102-smtp-auth-tls`

## Implementation Summary

The implementation adds TLS/STARTTLS support to the Papercut.Service SMTP server by:
1. Extending `EndpointDefinition` to support loading certificates from the Windows certificate store
2. Adding certificate configuration options to `SmtpServerOptions`
3. Updating the SMTP server initialization to use certificates when configured
4. Maintaining backward compatibility (TLS disabled by default)

## Changes Made

### 1. Core Domain Changes

#### [EndpointDefinition.cs](src/Papercut.Core/Domain/Network/EndpointDefinition.cs)

Added certificate support with secure loading from Windows certificate store:

```csharp
public X509Certificate? Certificate { get; }

public EndpointDefinition(
    string address,
    int port,
    X509FindType certificateFindType,
    string certificateFindValue,
    StoreLocation storeLocation = StoreLocation.LocalMachine,
    StoreName storeName = StoreName.My)
```

**Features**:
- Loads certificates from Windows certificate store (LocalMachine or CurrentUser)
- Supports multiple find types (Thumbprint, SubjectName, etc.)
- Validates exactly one certificate is found (prevents ambiguity)
- Enhanced `ToString()` to indicate TLS status
- Proper error messages for troubleshooting

### 2. Infrastructure Changes

#### [EndpointDefinitionBuilderExtensions.cs](src/Papercut.Infrastructure.Smtp/EndpointDefinitionBuilderExtensions.cs) *(NEW)*

Helper extension to bridge Papercut's `EndpointDefinition` with SmtpServer's `EndpointDefinitionBuilder`:

```csharp
public static EndpointDefinitionBuilder WithEndpoint(
    this EndpointDefinitionBuilder builder,
    EndpointDefinition smtpEndpoint)
```

- Automatically applies certificate if present
- Clean separation of concerns

#### [PapercutSmtpServer.cs](src/Papercut.Infrastructure.Smtp/PapercutSmtpServer.cs:105)

Updated to use the new extension method:

```csharp
.WithEndpoint(smtpEndpoint)  // Instead of .Endpoint(smtpEndpoint.ToIPEndPoint())
```

### 3. Service Configuration Changes

#### [SmtpServerOptions.cs](src/Papercut.Service/Domain/SmtpServer/SmtpServerOptions.cs)

Added four new configuration properties:

```csharp
public string CertificateFindType { get; set; } = "FindByThumbprint";
public string CertificateFindValue { get; set; } = string.Empty;
public string CertificateStoreLocation { get; set; } = "LocalMachine";
public string CertificateStoreName { get; set; } = "My";
```

**Defaults**: TLS disabled (empty `CertificateFindValue`)

#### [SmtpServerManager.cs](src/Papercut.Service/Infrastructure/Servers/SmtpServerManager.cs:85-135)

Enhanced `BindSMTPServer()` with conditional TLS configuration:

- Checks if certificate is configured (non-empty `CertificateFindValue`)
- Parses certificate store settings (with case-insensitive enum parsing)
- Creates `EndpointDefinition` with or without certificate
- Detailed logging for troubleshooting

## How It Works

### Without TLS (Default)

When `CertificateFindValue` is empty (default):

```json
{
  "IP": "Any",
  "Port": 25,
  "CertificateFindValue": ""
}
```

Server starts in plain SMTP mode (current behavior, fully backward compatible).

### With TLS/STARTTLS

When certificate is configured:

```json
{
  "IP": "Any",
  "Port": 587,
  "CertificateFindType": "FindByThumbprint",
  "CertificateFindValue": "ABC123DEF456...",
  "CertificateStoreLocation": "LocalMachine",
  "CertificateStoreName": "My"
}
```

Server:
1. Loads certificate from `LocalMachine\My` store by thumbprint
2. Configures SmtpServer with certificate
3. Enables STARTTLS command (`.IsSecure(false)` + certificate = STARTTLS mode)
4. Requires TLS before authentication (`.AllowUnsecureAuthentication(false)`)

## SMTP Authentication

Authentication is already implemented via [SimpleAuthentication.cs](src/Papercut.Infrastructure.Smtp/SimpleAuthentication.cs:24-29):

```csharp
public class SimpleAuthentication : IUserAuthenticatorFactory
{
    public IUserAuthenticator CreateInstance(ISessionContext context)
    {
        return new DelegatingUserAuthenticator((username, password) => true);
    }
}
```

**Current behavior**: Accepts all credentials (development/testing mode)

**Future enhancement**: Can be modified to validate against configured credentials or external auth.

## Configuration Guide

### Option 1: Using Certificate Thumbprint (Recommended)

1. Install certificate in Windows certificate store (LocalMachine\Personal)
2. Get the thumbprint (open cert → Details → Thumbprint)
3. Configure in `appsettings.json` or environment:

```json
{
  "CertificateFindType": "FindByThumbprint",
  "CertificateFindValue": "1234567890ABCDEF1234567890ABCDEF12345678"
}
```

### Option 2: Using Subject Name

```json
{
  "CertificateFindType": "FindBySubjectName",
  "CertificateFindValue": "localhost"
}
```

### Option 3: Using Distinguished Name

```json
{
  "CertificateFindType": "FindBySubjectDistinguishedName",
  "CertificateFindValue": "CN=localhost, O=Papercut"
}
```

## Creating Test Certificate

### PowerShell (Windows)

```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=localhost" `
    -DnsName "localhost" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(2) `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -FriendlyName "Papercut SMTP Dev Certificate" `
    -KeyUsage KeyEncipherment,DataEncipherment,DigitalSignature

# Get thumbprint
$cert.Thumbprint
```

### OpenSSL

```bash
# Create certificate
openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes

# Convert to PFX
openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in cert.pem

# Import to Windows store
certutil -f -p "password" -importpfx certificate.pfx
```

## Testing

### Test with Telnet (Plain SMTP)

```bash
telnet localhost 25
EHLO test.local
MAIL FROM:<test@example.com>
RCPT TO:<recipient@example.com>
DATA
Subject: Test
Test message
.
QUIT
```

### Test with OpenSSL (STARTTLS)

```bash
openssl s_client -connect localhost:587 -starttls smtp
```

Expected output should show:
- `250-STARTTLS` in EHLO response
- `250-AUTH PLAIN LOGIN` (after STARTTLS, if authentication enabled)

### Test with C# Client

```csharp
using MailKit.Net.Smtp;
using MimeKit;

var message = new MimeMessage();
message.From.Add(new MailboxAddress("Test Sender", "sender@test.com"));
message.To.Add(new MailboxAddress("Test Recipient", "recipient@test.com"));
message.Subject = "Test TLS";
message.Body = new TextPart("plain") { Text = "Testing STARTTLS" };

using var client = new SmtpClient();
client.Connect("localhost", 587, MailKit.Security.SecureSocketOptions.StartTls);
client.Authenticate("user", "pass");  // Will be accepted by SimpleAuthentication
client.Send(message);
client.Disconnect(true);
```

## SmtpServer Library Details

**Package**: SmtpServer 11.0.0 (by cosullivan)

**TLS Modes**:
- **Port 25**: Plain SMTP or opportunistic STARTTLS (if cert configured)
- **Port 587**: STARTTLS (recommended for submission, requires cert)
- **Port 465**: Immediate TLS (`.IsSecure(true)`)

**Current Configuration**: Port 587 mode (STARTTLS)
- `.IsSecure(false)` - Start unencrypted, allow STARTTLS upgrade
- `.Certificate(cert)` - Required to advertise STARTTLS
- `.AllowUnsecureAuthentication(false)` - Require TLS before AUTH

## Logging

The implementation includes detailed logging:

```
Information: Configuring SMTP server with TLS certificate: FindByThumbprint=ABC123... from LocalMachine\My
Information: TLS/STARTTLS support enabled for SMTP server
Information: Starting Smtp Server on 0.0.0.0:587 (with TLS)...
```

Or without TLS:

```
Information: SMTP server configured without TLS (plain text mode)
Information: Starting Smtp Server on 0.0.0.0:25...
```

## Error Handling

Certificate loading errors provide clear messages:

- **Not found**: `No certificate found matching FindByThumbprint='...' in LocalMachine\My store.`
- **Multiple found**: `Multiple certificates (3) found matching... Please provide a more specific search criteria.`
- **Parse error**: Exception with enum parse details

## Backward Compatibility

✅ **Fully backward compatible**
- Default configuration has empty `CertificateFindValue`
- Server starts in plain SMTP mode (existing behavior)
- No breaking changes to existing deployments
- TLS is opt-in via configuration

## Security Considerations

1. **Certificate Validation**: Server accepts self-signed certs (development mode)
   - See `IgnoreCertificateValidationFailureForTestingOnly` in [PapercutSmtpServer.cs:176-183](src/Papercut.Infrastructure.Smtp/PapercutSmtpServer.cs#L176-L183)

2. **Authentication**: Currently accepts all credentials
   - Appropriate for development/testing
   - Can be enhanced in `SimpleAuthentication.cs` for production

3. **Certificate Storage**: Uses Windows certificate store
   - Secure storage with ACLs
   - Centralized certificate management
   - Supports both LocalMachine and CurrentUser

## Future Enhancements

Potential improvements (not in this PR):

1. **UI Configuration** (Papercut.UI)
   - Add TLS settings to SMTP configuration dialog
   - Certificate selection dropdown
   - Test connection button

2. **Enhanced Authentication**
   - Configurable username/password validation
   - Integration with external auth systems
   - Per-user authentication logging

3. **Multiple Ports**
   - Support simultaneous plain (25), STARTTLS (587), and TLS (465)
   - Port-specific certificate configuration

4. **Certificate File Support**
   - Load from PFX file (in addition to store)
   - Encrypted password storage

5. **Certificate Auto-generation**
   - Auto-create self-signed cert on first run
   - Development mode convenience

## References

- **SmtpServer Library**: https://github.com/cosullivan/SmtpServer
- **Issue #102**: https://github.com/ChangemakerStudios/Papercut-SMTP/issues/102
- **Fork Implementation**: https://github.com/microalps/Papercut-SMTP/commit/e3f5e4f8a4ed633ea19d83815594101188df4026
- **RFC 3207 (STARTTLS)**: https://tools.ietf.org/html/rfc3207
- **RFC 4954 (SMTP AUTH)**: https://tools.ietf.org/html/rfc4954

## Files Modified

1. `src/Papercut.Core/Domain/Network/EndpointDefinition.cs` - Added certificate loading
2. `src/Papercut.Infrastructure.Smtp/EndpointDefinitionBuilderExtensions.cs` - NEW helper class
3. `src/Papercut.Infrastructure.Smtp/PapercutSmtpServer.cs` - Use new extension
4. `src/Papercut.Service/Domain/SmtpServer/SmtpServerOptions.cs` - Added cert config
5. `src/Papercut.Service/Infrastructure/Servers/SmtpServerManager.cs` - TLS initialization logic

## Testing Checklist

- [x] Builds without errors
- [ ] Plain SMTP works (port 25, no cert)
- [ ] STARTTLS works (port 587, with cert)
- [ ] Certificate not found error is clear
- [ ] Multiple certificates error is clear
- [ ] EHLO shows STARTTLS command
- [ ] EHLO shows AUTH command (after STARTTLS)
- [ ] Authentication works
- [ ] Emails are received and stored
- [ ] Logging is informative
- [ ] Settings persist across restarts

---

**Implementation Date**: 2025-10-20
**Developer**: Claude (Anthropic)
**Branch**: feature/102-smtp-auth-tls
