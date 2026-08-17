# MCP Server (AI Agents)

The Papercut SMTP Service includes an optional **[Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server** that lets AI agents and coding assistants — Claude Code, and any other MCP-capable client — inspect the email your application sends during development.

A typical agent-driven test loop:

1. The agent triggers your app to send an email
2. `list_messages` — confirm it arrived
3. `get_message` — assert on subject, recipients, and body
4. `get_message_section` — verify an attachment's actual content
5. `delete_all_messages` — reset for the next test

!!! note "Off by default"
    The MCP server is disabled unless you explicitly enable it. The service logs its status at startup either way:

    ```text
    [INF] MCP server is enabled -- serving MCP endpoint at /mcp
    [INF] MCP server is disabled (set EnableMcpServer to true to enable)
    ```

## Enabling

Set `EnableMcpServer` to `true` using any of the service's [configuration layers](service.md#configuration):

=== "appsettings.json"

    ```json
    { "EnableMcpServer": true }
    ```

=== "Environment variable"

    ```powershell
    $env:EnableMcpServer = 'true'
    ```

=== "Docker"

    ```bash
    docker run -d -p 8080:8080 -p 2525:2525 \
      -e EnableMcpServer=true \
      changemakerstudiosus/papercut-smtp:latest
    ```

Restart the service after changing it. When enabled, the endpoint is served at:

```text
http://localhost:8080/mcp
```

(Streamable HTTP transport; the path respects `HttpPathPrefix` if configured.) The web UI shows an **MCP** badge in the navigation bar when the server is on — hover it for the endpoint URL, click to copy. The URL is also available programmatically at `GET /api/mcp`, which — like all API routes — is relative to the service base URL including any `HttpPathPrefix`, and returns the full endpoint URL with the prefix applied.

## Connecting a client

**Claude Code:**

```bash
claude mcp add --transport http papercut http://localhost:8080/mcp
```

**Generic MCP client configuration:**

```json
{
  "mcpServers": {
    "papercut": {
      "type": "http",
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

## Tools

| Tool | Description |
|------|-------------|
| `list_messages` | Paged message summaries, newest first (`limit`, `start`) |
| `get_message` | Full detail for one message: from/to/cc/bcc, subject, text and HTML bodies, headers, and a manifest of MIME sections (index, contentId, media type, filename, attachment flag, size) |
| `get_message_section` | Decoded content of a single MIME part, selected by `index` or `contentId` from the manifest — text parts return as text, binary parts as base64 |
| `get_message_raw` | The raw RFC 822 (`.eml`) source of a message |
| `delete_message` | Delete one message by id |
| `delete_all_messages` | Clear the message store |

Large content is truncated to keep responses manageable (raw messages at 200K characters, binary sections at 512KB) with a `truncated` flag pointing to the full-content REST endpoints (`/api/messages/{id}/raw` and `/api/messages/{id}/sections/{index}`).

!!! warning "Network exposure"
    Like the REST API and web UI, the MCP endpoint has **no built-in authentication** — anyone who can reach the HTTP port can read and delete messages. Keep the binding on `localhost`, or put a reverse proxy with auth in front of it. See the [network exposure warning](service.md#configuration).
