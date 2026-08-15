# TLS & Authentication

Papercut supports **STARTTLS/TLS** and **SMTP AUTH** so you can test secure email flows end-to-end. Both are off by default and entirely optional.

## SMTP Authentication

Nothing to configure: if your application authenticates, Papercut **accepts any username and password**. Leave your app's production auth code path enabled and it just works.

## Enable TLS / STARTTLS

TLS activates when you give Papercut a certificate to use.

### 1. Create a test certificate

```powershell
New-SelfSignedCertificate -Subject "CN=localhost" -DnsName "localhost" `
    -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(2)
```

### 2. Point Papercut at it

In the service's `appsettings.json` (or `Papercut.Service.Settings.json`):

```json
{
  "SmtpServer": {
    "CertificateFindType": "FindBySubjectName",
    "CertificateFindValue": "localhost",
    "Port": 587
  }
}
```

Papercut finds the certificate in the Windows store and advertises STARTTLS. Port **587** is the conventional STARTTLS port, but any port works.

!!! tip "Self-signed certificates"
    Your app's SMTP client will likely reject a self-signed cert by default — most libraries have a dev-time escape hatch (e.g. Nodemailer's `tls: { rejectUnauthorized: false }`, MailKit's `ServerCertificateValidationCallback`).

## Configuration Reference

| Setting | Description | Default |
|---------|-------------|---------|
| `CertificateFindType` | Certificate search method (`FindBySubjectName`, `FindByThumbprint`, ...) | `FindBySubjectName` |
| `CertificateFindValue` | Certificate name/identifier — **empty disables TLS** | `""` |
| `CertificateStoreLocation` | `LocalMachine` or `CurrentUser` | `LocalMachine` |
| `CertificateStoreName` | Certificate store name | `My` |
| `Port` | SMTP port | `25` |

## Docker with TLS

Configure via environment variables and mount/reference your certificate:

```bash
docker run -d \
  -p 587:587 -p 8080:8080 \
  -e SmtpServer__CertificateFindType=FindBySubjectName \
  -e SmtpServer__CertificateFindValue=localhost \
  changemakerstudiosus/papercut-smtp:latest
```

See the [Docker Hub page](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp) for complete Docker Compose examples.
