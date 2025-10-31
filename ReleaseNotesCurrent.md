# Release Notes

## Papercut SMTP v7.5.1 [2025-10-31]

### Bug Fixes

- **Forwarding Rules Failing** - Fixed critical bug where email forwarding rules were completely broken due to TaskCanceledException. Fixes [#331](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/331) (Thanks, [wcwhitehead](https://github.com/wcwhitehead)!)
- **SSL/TLS Connection Mode** - Fixed SSL/TLS connection mode to use SslOnConnect for port 465 and Auto for STARTTLS
- **SMTP Timeout** - Set proper SMTP timeout of 30 seconds for forwarding rules
- **Backend Service Status** - Fixed initial status detection for backend service

### Contributors

Special thanks to [wcwhitehead](https://github.com/wcwhitehead) for reporting the critical forwarding rules bug!
