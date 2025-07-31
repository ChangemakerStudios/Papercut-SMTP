# Papercut SMTP - Working with Claude

## Project Overview

Papercut SMTP is a 2-in-1 solution that functions as both a quick email viewer and a built-in SMTP server designed to receive messages during development. It helps developers test email functionality without the risk of sending test emails to real recipients.

## Key Components

1. **Papercut UI** - Desktop application for viewing emails
2. **Papercut Service** - Optional HTTP server to receive emails even when the client is not running
3. **Docker Support** - Can be run in Docker for easy deployment

## Technology Stack

- **Framework**: .NET 8
- **UI Framework**: WPF with WebView2 for email display
- **Dependency Injection**: Autofac
- **Logging**: Serilog
- **SMTP Server**: SmtpServer library
- **Installation**: Velopack for auto-upgradable installation
- **Web UI**: Angular 17 with Angular Material and TailwindCSS

## Project Structure Breakdown

### Root Directories
- **/src** - Source code for all components
- **/build** - Build scripts and configuration
- **/test** - Test projects
- **/extras** - Additional resources
- **/graphics** - Images and graphics used in the application
- **/.github** - GitHub configuration files

### Core Projects

#### WPF Desktop UI Application
- **/src/Papercut.UI** - Main WPF application project
  - **/Views** - XAML view files
  - **/ViewModels** - View models following MVVM pattern
  - **/AppLayer** - Application layer services
  - **/Infrastructure** - Infrastructure components
  - **/Resources** - Resource files and themes
  - **/Helpers** - Helper classes
  - **/Domain** - Domain-specific logic

- **/src/Papercut.Desktop** - Desktop-specific functionality

#### Backend Service
- **/src/Papercut.Service** - Main service project (HTTP server)
  - **/Web** - Contains the Angular web UI
    - **/src** - Angular source code
    - **/App** - Application components
    - **/Assets** - Static assets
  - **electron.manifest.json** - Electron configuration
  - **appsettings.json** - Service configuration

#### Core Libraries
- **/src/Papercut.Core** - Core functionality shared between UI and Service
- **/src/Papercut.Common** - Common utilities and interfaces
- **/src/Papercut.Message** - Message handling and models
- **/src/Papercut.Rules** - Rules engine for message processing

#### Infrastructure Components
- **/src/Papercut.Infrastructure.Smtp** - SMTP server implementation
- **/src/Papercut.Infrastructure.IPComm** - Inter-process communication
- **/src/Papercut.App.WebApi** - Web API for service communication

#### Build & Deployment
- **/src/Papercut.Installer** - Installation packages
- **/src/Papercut.Bootstrapper** - Application bootstrapper

### Angular Web UI

The web UI is an Angular 17 application located in `/src/Papercut.Service/Web`. It uses:

- **Angular Material** (v17.3.10) - UI component library
- **TailwindCSS** (v3.4.17) - Utility-first CSS framework
- **RxJS** (v7.8.0) - Reactive programming library

The Angular application communicates with the Papercut Service backend via HTTP APIs to display and manage emails received by the SMTP server.

## Working with Claude on This Project

### Effective Assistance Areas

1. **Code Understanding and Navigation**
   - Understanding project structure and component relationships
   - Explaining specific code functionality
   - Identifying entry points and execution flow

2. **Feature Development**
   - Adding new rules for email processing
   - Enhancing UI components
   - Improving Docker configuration

3. **Troubleshooting**
   - Debugging email receiving/viewing issues
   - Resolving service startup problems
   - Fixing WebView2 integration issues

4. **Documentation**
   - Creating or improving documentation
   - Explaining configuration options
   - Documenting deployment scenarios

### Recent Changes (v7.0.0)

- Upgraded to .NET 8
- Upgraded to latest dependencies (Caliburn Micro, Autofac, MahApps)
- Switched to Velopack auto-upgradable installation system
- Fixed log updating constantly causing unnecessary WebView2 loading
- Fixed double request when clicking on links in emails
- Added InvokeProcess Rule

### Docker Usage

Papercut SMTP can be run in Docker with the following command:

```bash
docker run -d -p 8080:80 -p 25:25 changemakerstudiosus/papercut-smtp:latest
```

Build a custom Docker image:

```bash
./build-docker.sh <BUILD_VERSION>
```

### License

Papercut SMTP is licensed under the Apache License, Version 2.0.
