# Docker

The Papercut SMTP Service runs as a Linux container — ideal for docker-compose dev environments and CI pipelines.

[![Docker Pulls](https://img.shields.io/docker/pulls/changemakerstudiosus/papercut-smtp?logo=docker)](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)

## Quick Start

```bash
docker run -d --name papercut \
  -p 8080:8080 -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

- Web UI: **http://localhost:8080**
- Send email to: **localhost:2525**

!!! note "Container ports"
    The container uses non-privileged ports by default: SMTP **2525** and HTTP **8080**. Map them however you like — e.g. `-p 25:2525` if your app insists on port 25.

## Docker Compose

```yaml
services:
  papercut:
    image: changemakerstudiosus/papercut-smtp:latest
    ports:
      - "8080:8080"
      - "2525:2525"

  myapp:
    build: .
    environment:
      # inside the compose network, use the service name as SMTP host
      Smtp__Host: papercut
      Smtp__Port: "2525"
    depends_on:
      - papercut
```

Other containers on the same network reach Papercut at host **`papercut`**, port **`2525`**.

## Configuration via Environment Variables

Any setting can be overridden with environment variables:

```bash
docker run -d \
  -e SmtpServer__Port=2525 \
  -e Urls=http://0.0.0.0:8080 \
  -e HttpPathPrefix=/webmail \
  -p 8080:8080 -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

For TLS in Docker, see [TLS & Authentication](smtp-tls-auth.md).

## Tags

| Tag | Meaning |
|-----|---------|
| `latest` | Latest stable release |
| `X.Y.Z` / `X.Y` | Specific stable versions |
| `dev` | Latest development build |

## More

Full Docker documentation — volume persistence, Kubernetes examples, and troubleshooting (including volume permission errors) — lives on the [Docker Hub page](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp).

## .NET Aspire

For Aspire projects, skip raw Docker and use the community integration — `CommunityToolkit.Aspire.Hosting.PapercutSmtp`:

```csharp
var papercut = builder.AddPapercutSmtp("papercut");

builder.AddProject<Projects.MyApp>()
    .WithReference(papercut)
    .WaitFor(papercut);
```

Papercut appears in the Aspire dashboard with auto-assigned ports and a connection string of the form `endpoint=smtp://<host>:<port>`.
