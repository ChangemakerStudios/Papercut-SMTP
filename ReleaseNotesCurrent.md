# Release Notes

## Papercut SMTP v7.9.0 [2026-11-02]

### New Features

- **Rebuilt Web UI** - The Papercut Service web UI is rebuilt on Angular 21 and now matches the desktop app. It adds rules management, an Options dialog, a live service log viewer, Forward / Delete / Delete All, ctrl/shift multi-select, virtual scrolling with a message count, a sort direction toggle, read tracking, a System / Light / Dark theme, and a collapsible message list. Message HTML is rendered on the server through a MIME-aware pipeline, and emails keep their own colors in dark mode.
- **MCP Server** - Papercut Service can expose its messages to AI assistants over the Model Context Protocol. It is off by default; see [MCP](https://www.papercut-smtp.com/mcp/) in the docs.
- **SMTP Rate Limiting** - Optional, configurable rate limiting for incoming SMTP messages.
- **Documentation Site** - New docs at [papercut-smtp.com](https://www.papercut-smtp.com/). Fixes [#220](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/220) (Thanks, [Carsak](https://github.com/Carsak)!)

### Improvements

- **Service Tray Notifications** - The Service Tray Manager now gets new-mail notifications over SignalR. It treats any reachable service as running, and Open Web UI works for services that are not registered.
- **Rules Survive Updates** - The desktop app now saves `rules.json` to `%AppData%\Changemaker Studios\Papercut SMTP`, so updates no longer wipe your rules. **Velopack installs:** rules saved before this release are lost once, at this update (every earlier update already deleted them). Re-create your rules once after updating; they persist from now on.
- **Safer Message View** - `javascript:`, `data:` and `vbscript:` links are blocked, and links open in a new tab.
- **Bounded Memory** - Reading raw messages and MIME sections no longer loads unbounded data into memory.
- **WinGet** - Fixed the package ID and PR creation for WinGet publishing, and added ProductCode and ReleaseDate to the manifests. Relates to [#376](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/376) (Thanks, [AntekOlszewski](https://github.com/AntekOlszewski)!)
- **Cleanup** - Removed the unused Electron.NET integration. Added an Apache 2.0 LICENSE file.

### Bug Fixes

- **Forwarding Rules Corrupting Messages** - Fixed messages and attachments getting corrupted when several forwarding rules matched the same message. Each rule now works on its own copy read from disk. Fixes [#379](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/379) (Thanks for the report, [dpinkham6](https://github.com/dpinkham6)!)
- **Duplicate New-Message Events** - Each received message now raises one new-message event, not several.
- **Web UI Under a Path Prefix** - The Angular UI now loads correctly when the service runs under an `HttpPathPrefix`.
- **SMTP Rebind in the Service** - Changing the SMTP IP or port at runtime now moves the running listener. Before this fix, the old port stayed open and the new listener died.
- **Log File Access** - The log viewer opens the log file with shared access, so it no longer conflicts with the logger.

### Contributors

Special thanks to [dpinkham6](https://github.com/dpinkham6), [AntekOlszewski](https://github.com/AntekOlszewski), and [Carsak](https://github.com/Carsak) for their reports and suggestions!

Papercut SMTP is free and open source. If it saves you time, consider [sponsoring the project](https://github.com/sponsors/Jaben).

Several features and fixes in this release were developed with the assistance of [Claude Code](https://claude.ai/claude-code).
