# Papercut SMTP - Docker Guide

This guide covers running Papercut SMTP Service in Docker containers.

## Quick Start

```bash
# Pull the latest image
docker pull changemakerstudiosus/papercut-smtp:latest

# Run with default configuration (non-privileged ports)
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

Access the web UI at: **http://localhost:37408**

Send test emails to: **localhost:2525**

## Port Configuration

The Docker image uses **non-privileged ports by default**, allowing the container to run without root privileges:

| Service | Container Port | Default Host Mapping | Traditional Port |
|---------|---------------|---------------------|------------------|
| HTTP Web UI | 8080 | 37408 | 80 |
| SMTP Server | 2525 | 2525 | 25 |

### Why Non-Privileged Ports?

Ports below 1024 (like 25 and 80) require special permissions in Linux. By using ports 2525 and 8080, Papercut runs securely without needing:
- Root/administrator access
- `--sysctl` flags
- Special container capabilities

### Mapping to Traditional Ports

If you want the service available on traditional ports **on your host**, you can map them:

```bash
# Map to traditional ports on host (host needs appropriate privileges)
docker run -d \
  --name papercut \
  -p 80:8080 \
  -p 25:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Access:** http://localhost (port 80), SMTP on localhost:25

## Configuration

### Using Environment Variables

Override configuration using environment variables:

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
- `SmtpServer__IP` - SMTP listening address (default: "Any" which means 0.0.0.0)
- `SmtpServer__Port` - SMTP listening port (default: 2525)
- `SmtpServer__MessagePath` - Path where emails are stored
- `SmtpServer__LoggingPath` - Path for log files
- `Urls` - HTTP server URLs (default: http://0.0.0.0:8080)

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
    "MessagePath": "/app/messages",
    "LoggingPath": "/app/logs"
  }
}
```

## Persisting Email Messages

By default, emails are stored inside the container and will be lost when the container is removed. To persist emails, mount a volume:

```bash
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  -v papercut-messages:/app/Incoming \
  changemakerstudiosus/papercut-smtp:latest
```

Or use a host directory:

```bash
docker run -d \
  --name papercut \
  -p 37408:8080 \
  -p 2525:2525 \
  -v /path/on/host/messages:/app/Incoming \
  changemakerstudiosus/papercut-smtp:latest
```

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

## IPv6 Support

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

## Kubernetes Deployment

Example Kubernetes deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: papercut-smtp
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
        - containerPort: 2525
          name: smtp
        env:
        - name: SmtpServer__Port
          value: "2525"
        - name: Urls
          value: "http://0.0.0.0:8080"
        volumeMounts:
        - name: messages
          mountPath: /app/Incoming
      volumes:
      - name: messages
        emptyDir: {}
---
apiVersion: v1
kind: Service
metadata:
  name: papercut-smtp
spec:
  selector:
    app: papercut-smtp
  ports:
  - name: http
    port: 8080
    targetPort: 8080
  - name: smtp
    port: 2525
    targetPort: 2525
```

## Troubleshooting

### Port Permission Errors (Legacy)

If you see errors like:
```
Permission denied
```

This typically means you're trying to bind to privileged ports (<1024). Solutions:

1. **Use the default configuration** (ports 2525 and 8080) - Recommended
2. Map ports at the host level: `-p 25:2525 -p 80:8080`
3. Use `--sysctl net.ipv4.ip_unprivileged_port_start=0` (not recommended)

### Cannot Access Web UI

- Verify the container is running: `docker ps`
- Check the logs: `docker logs papercut`
- Ensure port mapping is correct
- Try accessing: `http://localhost:37408` or `http://localhost:8080` depending on your mapping

### SMTP Not Receiving Emails

- Verify SMTP port mapping: `docker port papercut`
- Test SMTP connection: `telnet localhost 2525`
- Check container logs: `docker logs papercut`
- Ensure firewall allows the SMTP port

### Configuration Not Taking Effect

The Docker image uses `ASPNETCORE_ENVIRONMENT=Production`, which loads `appsettings.Production.json` to override defaults. Make sure your environment variables or mounted configuration files are being picked up correctly.

Check the logs at startup to see the loaded configuration:
```bash
docker logs papercut | grep "SMTP Server Configuration Initialized"
```

## Building the Docker Image

To build the image yourself:

```bash
# From the repository root
docker build -t papercut-smtp:local .
```

## Related Documentation

- [Main README](README.md) - General Papercut information
- [Service README](src/Papercut.Service/Readme.md) - Service configuration details
- [GitHub Releases](https://github.com/ChangemakerStudios/Papercut-SMTP/releases) - Download desktop application
- [Docker Hub](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp) - Official Docker images
