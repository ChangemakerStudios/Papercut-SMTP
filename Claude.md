# Claude.md

This file provides context and guidelines for Claude Code when working with the Papercut SMTP project.

## Project Overview

Papercut SMTP is a 2-in-1 quick email viewer AND built-in SMTP server designed for development. It allows developers to safely test email functionality without risk of emails being sent to real recipients. The application provides:

- A desktop WPF application for viewing emails
- A built-in SMTP server (receive-only)
- An optional Windows Service for background email reception
- Full email inspection (body, HTML, headers, attachments, raw encoded bits)
- Support for running as a minimized tray application with notifications

**Tech Stack**: .NET 8.0, WPF, C#, MimeKit, Autofac (DI), Caliburn.Micro (MVVM), MahApps.Metro (UI), WebView2

## Architecture

The solution is organized into several projects with clear separation of concerns:

### Core Projects
- **Papercut.UI** - WPF desktop application (net8.0-windows)
  - Uses Caliburn.Micro for MVVM
  - MahApps.Metro for modern UI
  - WebView2 for HTML email rendering
  - ReactiveUI for reactive programming patterns

- **Papercut.Service** - Windows Service/HTTP server for receiving emails (net8.0)
  - ASP.NET Core based with embedded web UI
  - AngularJS web interface (embedded in Web/Assets/)
  - ElectronNET.API integration
  - Can run independently of the desktop UI
  - Accessible via HTTP (default port 8080)

- **Papercut.Core** - Core business logic and application services
  - Autofac dependency injection
  - Reactive extensions (System.Reactive)
  - Serilog logging

### Infrastructure Projects
- **Papercut.Infrastructure.Smtp** - SMTP protocol handling
- **Papercut.Infrastructure.IPComm** - Inter-process communication
- **Papercut.Message** - Email message parsing and file-based storage (`.eml` files)
- **Papercut.Rules** - Email rule processing
- **Papercut.Common** - Shared utilities and common code

### Email Storage
- Emails are stored as **`.eml` files** on disk (not in a database)
- Files are named with timestamp + subject + random string pattern
- MessageRepository handles file I/O and directory scanning
- Default storage path is configurable via MessagePathConfigurator

### Design Patterns
- Dependency Injection via Autofac throughout
- MVVM pattern in UI layer (Caliburn.Micro)
- Repository pattern for data access
- Observer pattern for email notifications
- Reactive programming with System.Reactive

## Important Conventions

- **Nullable reference types enabled** across all projects
- **Implicit usings enabled** for common namespaces
- **Global usings**: System.Text, Serilog, JetBrains.Annotations
- **Logging**: Use Serilog throughout (configured globally)
- **DI**: Constructor injection via Autofac
- **Code analysis**: JetBrains.Annotations used for nullability hints
- ReSharper settings in Papercut.sln.DotSettings

## Build and Test

### Build
```powershell
# Full build using Cake
.\build.ps1

# Or use standard dotnet commands
dotnet restore Papercut.sln
dotnet build Papercut.sln --configuration Release
```

### Cake Build Tasks
- `Clean` - Clean build artifacts
- `Restore` - Restore NuGet packages
- `BuildUI64` / `BuildUI32` - Build 64/32-bit UI
- `PackageUI64` / `PackageUI32` - Create Velopack installers
- `BuildAndPackServiceWin64` / `BuildAndPackServiceWin32` - Build service packages
- `All` - Complete build pipeline

### Docker
```powershell
# Build and run service in Docker
docker build -t papercut-smtp .
docker run -d -p 8080:80 -p 25:25 papercut-smtp:latest
```

### Versioning
- Uses GitVersion for semantic versioning
- Version properties defined in Directory.Build.props (populated by GitVersion during build)
- GitVersion.yml configures versioning strategy
- All version info (Version, AssemblyVersion, FileVersion, InformationalVersion) passed as MSBuild properties

## Common Tasks

<!-- To be filled in based on user input -->

## Areas of Focus

<!-- To be filled in based on user input -->

## Dependencies

### Key NuGet Packages
- **MimeKit** - Email parsing and MIME handling
- **Autofac** - Dependency injection container
- **Caliburn.Micro** - MVVM framework
- **MahApps.Metro** - WPF UI toolkit
- **WebView2** - Modern web rendering for HTML emails
- **Serilog** - Structured logging
- **SmtpServer** (Service only) - SMTP server implementation
- **Velopack** - Application packaging and updates
- **ReactiveUI** - Reactive extensions for UI

### Runtime Requirements
- .NET 8.0 Runtime
- Windows OS (WPF requirement)
- WebView2 Runtime (required for UI)
