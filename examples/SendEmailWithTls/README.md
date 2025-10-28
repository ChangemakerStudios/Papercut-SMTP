# SendEmailWithTls - TLS/STARTTLS Test Application

Example console application demonstrating TLS/STARTTLS connections and SMTP authentication with Papercut SMTP server.

## Purpose

This application tests the TLS/STARTTLS implementation for [issue #102](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/102).

## Prerequisites

1. **Papercut Service** running with TLS configured
2. **Certificate** installed in Windows certificate store
3. **.NET 8.0** runtime or SDK

## Configuration

Edit the constants in [Program.cs](Program.cs) to match your setup:

```csharp
const string SmtpHost = "localhost";
const int SmtpPort = 587;  // 25=plain, 587=STARTTLS, 465=TLS
const SecureSocketOptions Security = SecureSocketOptions.StartTls;
const string Username = "testuser";
const string Password = "testpass";
```

### SMTP Ports

| Port | Mode | Security Setting |
|------|------|-----------------|
| 25 | Plain SMTP | `SecureSocketOptions.None` |
| 587 | STARTTLS | `SecureSocketOptions.StartTls` |
| 465 | Immediate TLS | `SecureSocketOptions.SslOnConnect` |

## Running the Test

### From Command Line

```powershell
cd examples/SendEmailWithTls
dotnet run
```

### From Visual Studio

1. Open `SendEmailWithTls.csproj`
2. Press F5 to run
3. Or right-click project → Debug → Start New Instance

## Expected Output

```
=============================================================
Papercut SMTP - TLS/STARTTLS Connection Test
=============================================================
Server: localhost:587
Security: StartTls
Authentication: testuser
=============================================================

Test 1: TLS Connection and Authentication
-------------------------------------------------------------
  Connecting to localhost:587...
  Certificate Subject: CN=localhost
  Certificate Issuer: CN=localhost
  SSL Policy Errors: None
  ✓ Connection established

  Server Capabilities:
    - SIZE
    - STARTTLS
    - AUTH PLAIN LOGIN
    - ...

  Authenticating as 'testuser'...
  ✓ Authentication successful (IsAuthenticated: True)
  ✓ Disconnected successfully

Test 2: Send Test Email
-------------------------------------------------------------
  Connecting and sending email...
  ✓ Email sent successfully
  Server response: OK
  ✓ Disconnected successfully

=============================================================
✅ ALL TESTS PASSED
=============================================================
```

## Troubleshooting

### Connection Refused

**Error**: `Connection refused` on port 587

**Solution**:
- Ensure Papercut Service is running
- Verify TLS is configured (non-empty `CertificateFindValue`)
- Check port is correct in both app and server

### Certificate Error

**Error**: `No certificate found` or `Multiple certificates found`

**Solution**:
- Verify certificate is installed in correct store
- Use more specific search (e.g., Thumbprint instead of SubjectName)
- Check certificate permissions

### Authentication Failed

**Error**: `Authentication failed`

**Solution**:
- Papercut accepts all credentials by default
- Ensure STARTTLS completed before AUTH
- Check server logs for details

## What It Tests

1. ✅ **TLS/STARTTLS Connection** - Establishes encrypted connection
2. ✅ **Certificate Validation** - Displays certificate details
3. ✅ **SMTP Authentication** - Authenticates with username/password
4. ✅ **Email Sending** - Sends HTML and plain text email
5. ✅ **Server Capabilities** - Displays EHLO response

## Files

- `SendEmailWithTls.csproj` - Project file
- `Program.cs` - Main test application
- `README.md` - This file

## See Also

- [TLS_AUTH_IMPLEMENTATION.md](../../TLS_AUTH_IMPLEMENTATION.md) - Complete TLS implementation guide
- [docker-compose.example.yml](../../docker-compose.example.yml) - Docker TLS examples
- [SendEmailWithTLSTest.linq](../../linqpad/SendEmailWithTLSTest.linq) - LINQPad test script
