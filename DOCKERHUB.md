![Papercut Logo](https://raw.githubusercontent.com/ChangemakerStudios/Papercut/develop/graphics/PapercutLogo.png)<br>
*The Simple Desktop Email Helper*

## The problem
If you ever send emails from an application or website during development, you're familiar with the fear of an email being released into the wild. Are you positive none of the 'test' emails are addressed to colleagues or worse, customers? Of course, you can set up and maintain a test email server for development -- but that's a chore. Plus, the delay when waiting to view new test emails can radically slow your development cycle.

## Papercut SMTP to the rescue!
Papercut SMTP is a 2-in-1 quick email viewer AND built-in SMTP server (designed to receive messages only). Papercut SMTP doesn't enforce any restrictions on how you prepare your email, but it allows you to view the whole email: body, HTML, headers, and attachments. This Docker image runs the Papercut SMTP Service with an embedded web UI for viewing emails.

---

## Quick Start

```bash
# Pull the latest image
docker pull changemakerstudiosus/papercut-smtp:latest

# Run with default configuration
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Access the web UI at:** http://localhost:37408

**Send test emails to:** localhost:2525

---

## Port Configuration

The Docker image uses **non-privileged ports by default**, allowing the container to run securely without root privileges:

| Service | Container Port | Suggested Host Port | Traditional Port |
|---------|---------------|---------------------|------------------|
| HTTP Web UI | 8080 | 37408 | 80 |
| SMTP Server | 2525 | 2525 | 25 |

### Why Non-Privileged Ports?

Ports below 1024 (like 25 and 80) require special permissions in Linux. By using ports **2525** and **8080**, Papercut runs securely without needing:
- Root/administrator access
- `--sysctl` flags
- Special container capabilities

### Mapping to Traditional Ports

If you want the service available on traditional ports **on your host**, you can map them:

```bash
# Map to traditional ports on the host
docker run -d \
  --name papercut \
  -p 80:8080 \
  -p 25:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Access:** http://localhost (port 80) | **SMTP:** localhost:25

---

## Configuration

### Using Environment Variables

Override default configuration using environment variables:

```bash
docker run -d \
  --name papercut \
  -e SmtpServer__IP=0.0.0.0 \
  -e SmtpServer__Port=2525 \
  -e Urls=http://0.0.0.0:8080 \
  -p 8080:8080 \
  -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Available Environment Variables:**
- `SmtpServer__IP` - SMTP listening address (default: "Any" = 0.0.0.0)
- `SmtpServer__Port` - SMTP listening port (default: 2525, use 587 for STARTTLS)
- `SmtpServer__MessagePath` - Path where emails are stored (default: /app/Incoming)
- `SmtpServer__LoggingPath` - Path for log files (default: /app/logs)
- `Urls` - HTTP server URLs (default: http://0.0.0.0:8080)

**TLS/STARTTLS Configuration (Optional):**
- `SmtpServer__CertificateFindType` - Certificate search method (default: "FindBySubjectName")
- `SmtpServer__CertificateFindValue` - Certificate identifier (empty = TLS disabled)
- `SmtpServer__CertificateStoreLocation` - Store location: "LocalMachine" or "CurrentUser" (default: "LocalMachine")
- `SmtpServer__CertificateStoreName` - Store name: "My", "Root", etc. (default: "My")

### Using Custom Configuration File

Mount your own `appsettings.Production.json`:

```bash
docker run -d \
  --name papercut \
  -v /path/to/your/appsettings.Production.json:/app/appsettings.Production.json:ro \
  -p 8080:8080 \
  -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Example `appsettings.Production.json`:**
```json
{
  "Urls": "http://0.0.0.0:8080",

  "SmtpServer": {
    "IP": "Any",
    "Port": 2525,
    "MessagePath": "/app/Incoming",
    "LoggingPath": "/app/logs"
  }
}
```

---

## Persisting Email Messages

By default, emails are stored inside the container and will be lost when the container is removed. To persist emails, mount a volume:

### Using Docker Named Volume

```bash
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  -v papercut-messages:/app/Incoming \
  changemakerstudiosus/papercut-smtp:latest
```

### Using Host Directory

```bash
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  -v /path/on/host/messages:/app/Incoming \
  changemakerstudiosus/papercut-smtp:latest
```

---

## Docker Compose

Create a `docker-compose.yml`:

```yaml
version: '3.8'

services:
  papercut:
    image: changemakerstudiosus/papercut-smtp:latest
    container_name: papercut-smtp
    ports:
      - "37408:8080"  # Web UI
      - "2525:2525"   # SMTP
    volumes:
      - papercut-messages:/app/Incoming
      - papercut-logs:/app/logs
    environment:
      - SmtpServer__IP=0.0.0.0
      - SmtpServer__Port=2525
      - Urls=http://0.0.0.0:8080
    restart: unless-stopped

volumes:
  papercut-messages:
  papercut-logs:
```

Run with:
```bash
docker compose up -d
```

---

## TLS/STARTTLS and SMTP Authentication

Papercut SMTP supports optional TLS/STARTTLS encryption and SMTP authentication for secure email testing.

### Quick TLS Setup

**Enable STARTTLS** using environment variables:

```bash
docker run -d \
  --name papercut-tls \
  -p 8080:8080 \
  -p 587:587 \
  -e SmtpServer__Port=587 \
  -e SmtpServer__CertificateFindType=FindBySubjectName \
  -e SmtpServer__CertificateFindValue=localhost \
  changemakerstudiosus/papercut-smtp:latest
```

### Certificate Requirements

TLS/STARTTLS requires an X.509 certificate. The certificate must be installed in the Windows certificate store (LocalMachine or CurrentUser).

**Create a self-signed certificate for testing** (on Windows host):

```powershell
# Create certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=localhost" `
    -DnsName "localhost" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

# Get thumbprint for configuration
$cert.Thumbprint
```

Then use the thumbprint in your Docker configuration:

```bash
docker run -d \
  --name papercut-tls \
  -p 8080:8080 \
  -p 587:587 \
  -e SmtpServer__Port=587 \
  -e SmtpServer__CertificateFindType=FindByThumbprint \
  -e SmtpServer__CertificateFindValue=YOUR_THUMBPRINT_HERE \
  changemakerstudiosus/papercut-smtp:latest
```

### Docker Compose with TLS

```yaml
version: '3.8'

services:
  papercut-tls:
    image: changemakerstudiosus/papercut-smtp:latest
    container_name: papercut-smtp-tls
    ports:
      - "8080:8080"
      - "587:587"  # STARTTLS port
    volumes:
      - papercut-messages:/app/Incoming
      - papercut-logs:/app/logs
    environment:
      # Basic configuration
      - SmtpServer__IP=0.0.0.0
      - SmtpServer__Port=587
      - Urls=http://0.0.0.0:8080
      # TLS configuration
      - SmtpServer__CertificateFindType=FindByThumbprint
      - SmtpServer__CertificateFindValue=YOUR_CERT_THUMBPRINT
      - SmtpServer__CertificateStoreLocation=LocalMachine
      - SmtpServer__CertificateStoreName=My
    restart: unless-stopped

volumes:
  papercut-messages:
  papercut-logs:
```

### Configuration in appsettings.json

Alternatively, configure TLS in a custom `appsettings.Production.json`:

```json
{
  "Urls": "http://0.0.0.0:8080",

  "SmtpServer": {
    "IP": "Any",
    "Port": 587,
    "MessagePath": "/app/Incoming",
    "LoggingPath": "/app/logs",
    "CertificateFindType": "FindByThumbprint",
    "CertificateFindValue": "YOUR_CERT_THUMBPRINT_HERE",
    "CertificateStoreLocation": "LocalMachine",
    "CertificateStoreName": "My"
  }
}
```

### SMTP Ports Explained

| Port | Mode | Description |
|------|------|-------------|
| 25 | Plain SMTP | No encryption (default) |
| 587 | STARTTLS | Start plain, upgrade to TLS (recommended) |
| 465 | SMTPS | Immediate TLS encryption |
| 2525 | Plain SMTP | Non-privileged alternative to port 25 |

### Certificate Search Methods

| FindType | Example | Use Case | Ease of Use |
|----------|---------|----------|-------------|
| `FindBySubjectName` | `localhost` | Find by common name | ⭐⭐⭐ **Recommended** - Easiest |
| `FindByThumbprint` | `ABC123DEF456...` | Most specific | ⭐⭐ More secure but harder to configure |
| `FindBySubjectDistinguishedName` | `CN=localhost, O=Company` | Full distinguished name | ⭐ Most specific |

### SMTP Authentication

SMTP AUTH is automatically available when using TLS/STARTTLS. By default, Papercut accepts **all credentials** for development/testing purposes.

**Test with authentication:**

```bash
# Using openssl
openssl s_client -connect localhost:587 -starttls smtp

# Should see in EHLO response:
# 250-STARTTLS
# 250-AUTH PLAIN LOGIN
```

**Send email with MailKit (C#):**

```csharp
using var client = new SmtpClient();
client.Connect("localhost", 587, SecureSocketOptions.StartTls);
client.Authenticate("anyuser", "anypass");  // Accepts any credentials
client.Send(message);
client.Disconnect(true);
```

### Testing TLS Connection

```bash
# Test STARTTLS connection
openssl s_client -connect localhost:587 -starttls smtp

# Expected output should include:
# - STARTTLS in EHLO response
# - Certificate details
# - "Verify return code: 0 (ok)" or self-signed warning
```

### Troubleshooting TLS

**Certificate Not Found:**
```
Error: No certificate found matching FindByThumbprint='...'
```
- Verify certificate is installed in the correct store
- Check thumbprint is correct (remove spaces)
- Ensure container has access to host certificate store

**Multiple Certificates Found:**
```
Error: Multiple certificates (3) found matching...
```
- Use a more specific search method (e.g., thumbprint instead of subject name)

**Connection Refused on Port 587:**
- Ensure port mapping includes 587: `-p 587:587`
- Verify TLS is configured (non-empty CertificateFindValue)
- Check logs: `docker logs papercut-tls | grep TLS`

---

## Advanced Examples

### IPv6 Support

To listen on IPv6:

```bash
docker run -d \
  --name papercut \
  -e SmtpServer__IP=::0 \
  -e Urls=http://[::]:8080 \
  -p 8080:8080 \
  -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

### Custom Ports

To use completely custom ports:

```bash
docker run -d \
  --name papercut \
  -e SmtpServer__Port=3025 \
  -e Urls=http://0.0.0.0:9080 \
  -p 9080:9080 \
  -p 3025:3025 \
  changemakerstudiosus/papercut-smtp:latest
```

### With Persistent Storage and Custom Config

```bash
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  -v papercut-messages:/app/Incoming \
  -v papercut-logs:/app/logs \
  -v $(pwd)/appsettings.Production.json:/app/appsettings.Production.json:ro \
  changemakerstudiosus/papercut-smtp:latest
```

---

## Kubernetes Deployment

Example Kubernetes deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: papercut-smtp
  labels:
    app: papercut-smtp
spec:
  replicas: 1
  selector:
    matchLabels:
      app: papercut-smtp
  template:
    metadata:
      labels:
        app: papercut-smtp
    spec:
      containers:
      - name: papercut
        image: changemakerstudiosus/papercut-smtp:latest
        ports:
        - containerPort: 8080
          name: http
          protocol: TCP
        - containerPort: 2525
          name: smtp
          protocol: TCP
        env:
        - name: SmtpServer__Port
          value: "2525"
        - name: Urls
          value: "http://0.0.0.0:8080"
        volumeMounts:
        - name: messages
          mountPath: /app/Incoming
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: messages
        emptyDir: {}
      - name: logs
        emptyDir: {}
---
apiVersion: v1
kind: Service
metadata:
  name: papercut-smtp
  labels:
    app: papercut-smtp
spec:
  type: ClusterIP
  selector:
    app: papercut-smtp
  ports:
  - name: http
    port: 8080
    targetPort: 8080
    protocol: TCP
  - name: smtp
    port: 2525
    targetPort: 2525
    protocol: TCP
```

Apply with:
```bash
kubectl apply -f papercut-deployment.yaml
```

Access via port-forward for testing:
```bash
kubectl port-forward svc/papercut-smtp 37408:8080 2525:2525
```

---

## Troubleshooting

### Port Permission Errors

**Error:** `Permission denied` when binding to ports

**Solution:** Use the default non-privileged ports (2525, 8080) or map ports at the host level:
```bash
# Correct - map at host level
docker run -d -p 25:2525 -p 80:8080 changemakerstudiosus/papercut-smtp:latest

# Avoid - requires special permissions
docker run -d --sysctl net.ipv4.ip_unprivileged_port_start=0 changemakerstudiosus/papercut-smtp:latest
```

### Cannot Access Web UI

**Symptoms:** Cannot connect to http://localhost:37408

**Checks:**
1. Verify container is running: `docker ps`
2. Check logs: `docker logs papercut`
3. Verify port mapping: `docker port papercut`
4. Try alternate port based on your mapping: http://localhost:8080

### SMTP Not Receiving Emails

**Symptoms:** Emails not appearing in Papercut

**Checks:**
1. Test SMTP connection: `telnet localhost 2525`
2. Verify SMTP port mapping: `docker port papercut`
3. Check container logs: `docker logs papercut | grep SMTP`
4. Verify firewall allows the port
5. Ensure your application is sending to the correct port (2525, not 25)

### Configuration Not Taking Effect

**Symptoms:** Environment variables or config changes ignored

**Checks:**
1. Verify the container is using Production environment:
   ```bash
   docker logs papercut | grep ASPNETCORE_ENVIRONMENT
   ```
2. Check that configuration was loaded:
   ```bash
   docker logs papercut | grep "SMTP Server Configuration Initialized"
   ```
3. Ensure environment variable format is correct (use double underscores: `__`)

### Container Exits Immediately

**Symptoms:** Container starts then immediately stops

**Checks:**
1. View logs: `docker logs papercut`
2. Verify WebView2 or other dependencies aren't causing issues (should not affect service)
3. Check for port conflicts with existing services

---

## Testing Your Setup

### Send a Test Email via Command Line

**Linux/Mac:**
```bash
echo -e "Subject: Test Email\n\nThis is a test message." | nc localhost 2525
```

**Windows PowerShell:**
```powershell
$smtp = New-Object Net.Mail.SmtpClient("localhost", 2525)
$smtp.Send("test@example.com", "recipient@example.com", "Test Subject", "Test body")
```

### Send Test Email via Telnet

```bash
telnet localhost 2525
EHLO localhost
MAIL FROM:<sender@example.com>
RCPT TO:<recipient@example.com>
DATA
Subject: Test Email

This is a test message.
.
QUIT
```

---

## Links

- **GitHub Repository:** https://github.com/ChangemakerStudios/Papercut-SMTP
- **Desktop Application:** https://github.com/ChangemakerStudios/Papercut-SMTP/releases
- **Documentation:** https://github.com/ChangemakerStudios/Papercut-SMTP/tree/develop/src/Papercut.Service
- **Issues:** https://github.com/ChangemakerStudios/Papercut-SMTP/issues

---

## License

Papercut SMTP is licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
