# Release Notes

## Papercut SMTP v7.5.0 [2025-10-28]

### New Features

- **Search Functionality** - Added Ctrl+F search to all message detail views (Message, Body, Headers, and Raw). Fixes [#295](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/295)
- **Zoom Controls** - Added Ctrl+Mouse Scroll Wheel zoom support to message detail views with persistence and visual indicators. Fixes [#323](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/323)
- **Attachment Display** - Attachments now display in the main message panel with icons and easy access. Fixes [#112](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/112)
- **Context Menu for Links** - Right-click context menu added for email links with copy and open functionality. Fixes [#218](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/218)
- **Empty State Placeholders** - Added smart empty state messaging to message list and detail views for better first-time user experience. Fixes [#268](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/268)
- **SMTP Authentication and TLS/STARTTLS Support** - Full support for SMTP authentication and TLS/STARTTLS encryption for secure email testing. Fixes [#102](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/102)
- **Periodic Background Rules** - Added periodic background rule execution with mail retention cleanup to both UI and Service. Related to [#251](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/251)
- **Windows Package Manager (winget) Support** - Papercut SMTP can now be installed and updated via winget. Fixes [#231](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/231)
- **File Links Support** - Made file:/// links functional by opening with shell/explorer. Fixes [#232](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/232)
- **Example Console Applications** - Added comprehensive example console applications for email testing scenarios
- **Installer Help Support** - Added help parameter support for PowerShell/cmd users. Fixes [#287](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/287)

### Improvements

- **Word-Wrap Support** - Added CSS word-wrap support to HTML email rendering for better display of long text strings. Fixes [#154](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/154)
- **Non-Standard Email Domains** - Added support for non-standard email domains in development environments (e.g., .local, .dev). Fixes [#284](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/284)
- **SSL Certificate Error Handling** - Added optional SSL certificate error handling for WebView2 to support self-signed certificates. Fixes [#243](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/243)
- **Comprehensive Unit Tests** - Added extensive unit tests with FluentAssertions for improved code quality and reliability
- **NuGet Package Upgrades** - Upgraded all NuGet packages to latest versions, including migration to new Polly ResiliencePipeline API
- **Service Architecture** - Refactored attachment and process handling into dedicated services for better separation of concerns
- **Settings Debouncing** - Implemented debounced settings saves using Rx Throttle operator to reduce unnecessary I/O

### Bug Fixes

- **Window Size Saving** - Fixed window size not saving correctly when minimized. Fixes [#327](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/327)
- **PDF Attachment Opening** - Fixed PDF attachment opening on Windows 11 24H2 and double-click issues. Fixes [#310](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/310), [#280](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/280)
- **Message Rendering with # Characters** - Fixed message rendering for subjects containing # characters. Fixes [#282](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/282) (Thanks, [xavero](https://github.com/xavero)!)
- **SMTP IP Logging** - Fixed SMTP service not logging remote client IP address correctly. Fixes [#291](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/291)
- **Empty State Flicker** - Fixed placeholder flicker when switching between emails
- **WebView2 Deserialization** - Added robust error handling for WebView2 message deserialization
- **Null Safety** - Various null safety improvements and fixes throughout the codebase
- **Process.Start Null Checks** - Added null checks for Process.Start return values to prevent exceptions

### Code Quality

- Refactored to use IUiCommandHub.ShowMessage instead of MessageBox.Show for better testability
- Extracted debouncing logic into reusable SettingsSaveDebouncer
- Improved async patterns and error handling in example applications
- Fixed test assertions to use FluentAssertions consistently
- Added nullable reference type attributes throughout codebase

### Documentation

- Added comprehensive TLS/STARTTLS setup documentation with Docker examples
- Created example projects: SendEmailTest, SendRichEmailTest, SendWordWrapTest, SendEmailWithTls, SendEmailWithFileLinks
- Improved Docker documentation with troubleshooting guides
- Added installation guide for PowerShell and command-line users

### Contributors

Special thanks to [xavero](https://github.com/xavero) for fixing message rendering with # characters!
