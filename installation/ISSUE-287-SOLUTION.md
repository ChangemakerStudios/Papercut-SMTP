# Issue #287 - Installer Help Parameter Support

## Problem Summary

GitHub Issue: https://github.com/ChangemakerStudios/Papercut-SMTP/issues/287

**Issue**: When running `Setup.exe --help` or `-h` from PowerShell/cmd, no output is displayed, making it difficult for users to discover installation options for unattended installs.

## Root Cause Analysis

The VeloPack Setup.exe **does support** `--help` and `-h` parameters as of version 0.0.1053+. However, there's a fundamental Windows limitation:

- Setup.exe is compiled as a **WinExe** (GUI application) to avoid showing a console window during installation
- Windows GUI applications cannot attach to an existing console window in PowerShell/cmd
- When `--help` is called, the help text is generated but has nowhere to display

### Version History

- **v7.0.1**: Used vpk 0.0.359 (older version with limited help support)
- **v7.0.2**: Upgraded to vpk 0.0.1298 (includes full --help support)
- **Current**: vpk 0.0.1298 (latest)

### Behavior Comparison

| Command | Git Bash/WSL | PowerShell/cmd |
|---------|--------------|----------------|
| `--help` | ✓ Works | ✗ No output |
| `-h` | ✓ Works | ✗ No output |
| `--?` | ✗ Error dialog | ✗ Error dialog |

## Solution Implemented

Since we cannot change how VeloPack's Setup.exe is compiled, we've implemented a multi-pronged solution:

### 1. PowerShell Helper Script (`Install-PapercutSMTP.ps1`)

A PowerShell script that wraps Setup.exe and provides:
- Native PowerShell help support (`-Help`, `-h`, `-?`)
- PowerShell-friendly parameter names
- Color-coded help output
- Proper exit codes

**Usage:**
```powershell
.\Install-PapercutSMTP.ps1 -Help
.\Install-PapercutSMTP.ps1 -Silent
.\Install-PapercutSMTP.ps1 -InstallTo "C:\Apps\PapercutSMTP"
```

### 2. Installation Guide (`INSTALLATION.md`)

Comprehensive documentation covering:
- All installation parameters
- Examples for common scenarios
- Workarounds for PowerShell console limitations
- CI/CD integration examples
- Troubleshooting tips

### 3. Build System Updates (`build.cake`)

Modified PackageUI64, PackageUI32, and PackageUIArm64 tasks to automatically copy helper files alongside Setup.exe in the releases directory.

### 4. Splash Screen Asset (`InstallSplash.svg`)

Created an SVG template for a future custom splash screen that displays installation parameters during the install process (optional enhancement).

## Files Changed

### New Files
- `extras/Install-PapercutSMTP.ps1` - PowerShell wrapper script
- `extras/INSTALLATION.md` - Installation guide
- `src/Papercut.UI/Resources/InstallSplash.svg` - Splash screen template
- `ISSUE-287-SOLUTION.md` - This document

### Modified Files
- `build.cake` - Added helper file copying to package tasks (lines 167-169, 213-215, 259-261)

## Workarounds for Direct Setup.exe Usage

If users want to use Setup.exe directly from PowerShell:

### Option 1: Use --log parameter
```powershell
.\Setup.exe --help --log help.txt
type help.txt
```

### Option 2: Use Git Bash/WSL
```bash
./Setup.exe --help
```

### Option 3: Use the PowerShell helper
```powershell
.\Install-PapercutSMTP.ps1 -Help
```

## Testing

Tested scenarios:
- ✓ PowerShell helper script `-Help`, `-h`, `-?` parameters
- ✓ Setup.exe `--help` in Git Bash (works correctly)
- ✓ Setup.exe `--help` in PowerShell (no output, as expected)
- ✓ Build.cake file copying

## Recommendations

1. **Document in Release Notes**: Mention the new PowerShell helper script in the next release
2. **Update GitHub README**: Add installation instructions pointing to `Install-PapercutSMTP.ps1`
3. **Optional**: Convert `InstallSplash.svg` to PNG and add to build with `--splashImage` parameter

## GitHub Issue Resolution

The issue can be closed with a comment explaining:

1. The underlying Windows GUI limitation
2. The workarounds we've implemented
3. That `--help` works in Git Bash/WSL but not natively in PowerShell/cmd
4. How to use the new `Install-PapercutSMTP.ps1` helper script

## Sample GitHub Comment

```markdown
This issue has been addressed in the `feature/287-installer-help-parameter` branch.

## The Problem

Setup.exe does support `--help` and `-h`, but because it's compiled as a Windows GUI application (to avoid showing a console during installation), the help output cannot be displayed in PowerShell or cmd.exe.

## The Solution

We've added two helper files that will be included with future releases:

1. **Install-PapercutSMTP.ps1** - A PowerShell wrapper that provides native help support
2. **INSTALLATION.md** - Comprehensive installation documentation

## Usage

```powershell
# Show help
.\Install-PapercutSMTP.ps1 -Help

# Silent install
.\Install-PapercutSMTP.ps1 -Silent
```

## Direct Setup.exe Workarounds

If you need to use Setup.exe directly:

```powershell
# Save help to file
.\Setup.exe --help --log help.txt
type help.txt

# Or use Git Bash/WSL
bash -c "./Setup.exe --help"
```

This will be available in the next release (7.0.3+).
```
