# Release Notes

## Papercut SMTP v7.0.2 [2025-10-09]

### Improvements
- Modernized versioning system to use Directory.Build.props instead of GlobalAssemblyInfo.cs
- Added Windows ARM64 support (#283)
- Improved port configuration for SMTP and HTTP services (#285, #286, #293)
- Enhanced Docker support with non-privileged ports (SMTP: 2525, HTTP: 8080)
- Consolidated documentation and improved Docker build process

### Bug Fixes
- Fixed Windows Service installation and auto-start issues (#294)
- Fixed Linux path separator handling (#292)
- Fixed SMTP server options configuration

### Build System
- Refactored build.cake to reduce code duplication
- GitVersion now populates version properties via MSBuild
- Removed deprecated assembly patching approach
