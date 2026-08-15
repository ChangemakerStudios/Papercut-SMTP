# Release Notes

## Papercut SMTP v7.8.0 [2026-08-15]

### New Features

- **SMTP Authentication for Forwarding** - The Forward dialog and forwarding rules now support SMTP username/password authentication, along with Forward dialog usability improvements and a new Name property for rules. Fixes [#363](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/363) (Thanks for the report, [Theo-bos](https://github.com/Theo-bos)!)
- **Configurable Web UI Path Prefix** - The Papercut Service web UI and API can now be hosted under a configurable HTTP path prefix, enabling reverse proxy scenarios (e.g. `https://myserver/papercut/`). Fixes [#365](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/365) (Thanks for the suggestion, [Moreno-Gentili](https://github.com/Moreno-Gentili)!)

### Improvements

- **.NET 10 Upgrade** - All projects upgraded from .NET 8 to .NET 10 for the latest runtime performance and security improvements. (Thanks, [Abdulstar](https://github.com/Abdulstar)!)
- **Service Tray Manager Deployment** - The Service Tray Manager introduced in v7.7.2 is now included in the Windows Service deployment packages
- **Smaller UI Footprint** - Removed unused MahApps icon pack dependencies, reducing application size. Fixes [#370](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/370) (Thanks, [AntekOlszewski](https://github.com/AntekOlszewski)!)
- **Documentation** - Added Web UI screenshots and fixed the README star history chart (Thanks, [PingouinFerreux](https://github.com/PingouinFerreux)!)

### Bug Fixes

- **Message List Stuck After Long Uptime** - Fixed the desktop app's message list permanently stopping to update after running for a long time (new emails triggered notifications but never appeared until restart). A single transient file-system error could silently terminate the internal refresh subscriptions; refreshes are now resilient to errors, and the message list additionally force-refreshes whenever the window is restored from the tray or taskbar
- **Rule Sync to Service** - Fixed rule changes made in the desktop UI not being saved to the Papercut Service backend when connected. Fixes [#368](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/368) (Thanks for the report, [Pxtl](https://github.com/Pxtl)!)
- **Periodic Rules in Standalone Service** - Fixed periodic background rules (such as the Mail Retention rule) never executing when running the standalone Papercut Service. Fixes [#369](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/369) (Thanks for the report, [Pxtl](https://github.com/Pxtl)!)

### Contributors

Special thanks to [Abdulstar](https://github.com/Abdulstar) for the .NET 10 upgrade, [AntekOlszewski](https://github.com/AntekOlszewski) for the icon pack cleanup, [PingouinFerreux](https://github.com/PingouinFerreux) for the README fix, and to [Theo-bos](https://github.com/Theo-bos), [Moreno-Gentili](https://github.com/Moreno-Gentili), and [Pxtl](https://github.com/Pxtl) for their reports and suggestions!

Several features and fixes in this release were developed with the assistance of [Claude Code](https://claude.ai/claude-code).
