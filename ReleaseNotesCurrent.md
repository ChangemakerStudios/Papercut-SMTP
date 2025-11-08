# Release Notes

## Papercut SMTP v7.6.1 [2025-11-08]

### New Features

- **Dark Theme Support** - Added full dark theme support with automatic Windows dark mode detection and synchronization. Fixes [#228](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/228) (Thanks, [wonea](https://github.com/wonea)!)
  - System/Light/Dark base theme selection with accent color customization
  - Automatic system dark mode detection with live theme updates
  - AvalonEdit syntax highlighting supports both light and dark modes
  - Theme-aware attachment icons and UI controls throughout the application
- **IP Allowlist Support** - Added IP allowlist filtering for SMTP and HTTP connections with CIDR notation support. Fixes [#333](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/333) (Thanks, [mhkarimi1383](https://github.com/mhkarimi1383)!)
  - Configurable via `SmtpServer:AllowedIps` setting
  - Support for individual IPs (e.g., `192.168.1.100`) and CIDR ranges (e.g., `192.168.1.0/24`)
  - Comprehensive unit tests for IP validation and range matching
  - Fail-closed security - blocks all connections if validation fails
- **Dynamic Property Editor for Rules** - Replaced legacy PropertyGrid with custom MahApps.Metro-compatible dynamic property editor
  - Reflection-based property discovery using System.ComponentModel attributes
  - Intelligent control selection (TextBox, NumericUpDown, ToggleSwitch, PasswordBox)
  - Category-based property organization with smart ordering
  - Resizable GridSplitter between rules list and property editor
  - Full dark theme support in Rules Configuration dialog

### Improvements

- **.NET 9.0 Upgrade** - Upgraded entire solution to .NET 9.0 for improved performance and latest framework features
- **Theme Consistency** - Modernized UI to use dynamic theming with refreshed Options dialog and message list visuals
- **Tab Preservation** - Selected message detail tab (Message/Body/Headers/Raw) now preserved when switching between messages
- **Code Quality** - Fixed numerous compiler warnings and null-safety issues throughout the codebase
- **Resource Management** - Fixed X509Certificate2Collection resource leak
- **Docker Improvements** - Updated container and runtime images for .NET 9.0

### Code Quality

- Comprehensive unit tests for IP allowlist validation
- More flexible SMTP/server startup wiring and options
- Internal robustness improvements with clearer initialization and lifecycle handling
- Better error handling across core components

### Contributors

Special thanks to [wonea](https://github.com/wonea) for requesting dark theme support and [mhkarimi1383](https://github.com/mhkarimi1383) for requesting IP allowlist functionality!
