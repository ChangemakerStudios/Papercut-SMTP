# Papercut.Service

**Papercut.Service** is a modern ASP.NET Core 8.0 web application that serves as the backend service for Papercut SMTP. It provides both SMTP email capture functionality and a RESTful API for managing messages, with an optional Electron-based desktop interface.

## 🎯 Purpose & Architecture

Papercut.Service is designed as a **hybrid application** that can operate in multiple modes:

- **Standalone Service**: Runs as a background Windows service for server deployments
- **Desktop Application**: Optionally runs with an Electron GUI for desktop usage
- **Web API Service**: Always provides HTTP endpoints for message management
- **SMTP Server**: Captures SMTP messages sent to configurable ports

## 🏗️ Architecture & Code Organization

Papercut.Service follows **Domain-Driven Design (DDD)** principles with a clean, feature-based architecture organized into three main layers:

### 📁 **DDD Layer Structure**

```
src/Papercut.Service/
├── Application/           # Application Layer - Features & Use Cases
│   ├── Messages/         # Message management feature
│   │   ├── MessagesController.cs
│   │   ├── MessagesHub.cs
│   │   ├── NewMessageEventHandler.cs
│   │   ├── MimePartFileStreamResult.cs
│   │   └── Models/       # Message-related DTOs and models
│   ├── Health/           # Health check feature
│   │   └── HealthController.cs
│   ├── SmtpServer/       # SMTP server configuration
│   │   └── Models/       # SMTP configuration models
│   └── WebUI/            # Web interface feature
│       └── StaticContentController.cs
├── Domain/               # Domain Layer - Business Logic & Entities
│   └── Notification/     # Domain events and notifications
└── Infrastructure/       # Infrastructure Layer - External Concerns
    ├── EmailAddresses/   # Email address utilities
    ├── Hosting/          # ASP.NET hosting configuration
    ├── IPComm/           # Inter-process communication
    ├── Logging/          # Logging infrastructure
    ├── Paths/            # File path management
    ├── Rules/            # Rule processing infrastructure
    └── Servers/          # Server management
```

### 🎯 **Layer Responsibilities**

#### **Application Layer**
- **Purpose**: Orchestrates business operations and handles external requests
- **Contents**: Controllers, SignalR hubs, application services, DTOs
- **Organization**: Grouped by **business feature** (Messages, Health, SmtpServer, WebUI)
- **Dependencies**: Can depend on Domain and Infrastructure layers

#### **Domain Layer** 
- **Purpose**: Contains core business logic, entities, and domain events
- **Contents**: Domain entities, value objects, domain services, events
- **Organization**: Grouped by **domain concept** (Notification, etc.)
- **Dependencies**: Should be independent (no dependencies on other layers)

#### **Infrastructure Layer**
- **Purpose**: Implements technical concerns and external system integrations
- **Contents**: Data access, external APIs, file systems, logging, hosting
- **Organization**: Grouped by **technical capability** (Logging, Hosting, etc.)
- **Dependencies**: Can depend on Domain layer for interfaces

### 🔄 **Feature-Based Organization Benefits**

✅ **High Cohesion** - Related code stays together by business purpose
✅ **Low Coupling** - Clear boundaries between features and layers  
✅ **Team Ownership** - Teams can own entire vertical slices
✅ **Independent Evolution** - Features can be modified without affecting others
✅ **Easy Navigation** - Intuitive file organization by business domain
✅ **Testability** - Clear boundaries make unit and integration testing focused

### 🏗️ **Core Components**

#### **Backend Infrastructure**
- **ASP.NET Core 8.0** - Modern web application framework
- **Autofac IoC Container** - Dependency injection and modular architecture
- **Serilog Logging** - Structured logging with multiple sinks
- **SmtpServer Library** - SMTP protocol implementation
- **MimeKit** - Email message parsing and handling

#### **Key Modules & Dependencies**
- **Papercut.Core** - Core domain logic and infrastructure
- **Papercut.Infrastructure.Smtp** - SMTP server implementation
- **Papercut.Infrastructure.IPComm** - Inter-process communication
- **Papercut.Message** - Message repository and management
- **Papercut.Rules** - Message processing rules engine
- **Papercut.Common** - Shared utilities and extensions

#### **Optional Desktop Support**
- **ElectronNET.API** - Cross-platform desktop wrapper (when enabled)
- **Angular 17 Web UI** - Modern frontend embedded as static assets

## 🚀 How to Run Papercut.Service

### Option 1: Command Line Execution
```bash
Papercut.Service.exe
```

### Option 2: Windows Service Installation
```bash
# Install and start as Windows service
Papercut.Service.exe install --sudo
```

### Option 3: With Electron Desktop Interface
The service can optionally launch with a desktop GUI when Electron support is active. This provides a native desktop experience while maintaining the same backend functionality.

### Command Line Options
Get a full listing of available command line options:
```bash
Papercut.Service.exe help
```

## ⚙️ Configuration

### Primary Configuration File
The service uses standard ASP.NET Core configuration files:
- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides

### SMTP Server Configuration
```json
{
  "SmtpServer": {
    "IP": "Any",
    "Port": 25,
    "MessagePath": "%BaseDirectory%\\Incoming",
    "LoggingPath": "%DataDirectory%\\Logs;%BaseDirectory%\\Logs"
  }
}
```

### Automatic Synchronization
When running alongside the Papercut desktop client (`Papercut.exe`), configurations automatically synchronize:
- SMTP settings changes in the UI are reflected in the service
- Rule modifications are shared between client and service
- No manual configuration synchronization required

## 🔌 API Endpoints

The service exposes a RESTful API for message management:

### Message Operations
- `GET /api/messages` - Retrieve paginated message list
- `GET /api/messages/{id}` - Get detailed message content
- `DELETE /api/messages` - Delete all messages
- `GET /api/messages/{id}/raw` - Download raw message file
- `GET /api/messages/{id}/sections/{index}` - Download message attachments

### Health Check
- `GET /health` - Service health status

### Features
- **ETag Support** - Efficient caching and conditional requests
- **Pagination** - Configurable page sizes for large message lists
- **CORS Enabled** - Cross-origin requests supported
- **File Downloads** - Raw message and attachment downloads

## 🌐 Web UI Integration

The service includes an embedded **Angular 17** web interface located in the `Web/` directory:

- **Modern Angular Architecture** - Standalone components, signals, and latest features
- **Material Design** - Angular Material UI components
- **Tailwind CSS** - Utility-first styling framework
- **Real-time Updates** - Live message monitoring and management
- **Responsive Design** - Mobile-friendly interface

**Note**: The Web UI is embedded as static assets and served by the ASP.NET Core application. See `Web/README.md` for detailed frontend documentation.

## 🖥️ Electron Desktop Mode

**Status**: Currently available but not actively used in production deployments.

The Electron integration (`ElectronService.cs`) provides:
- Cross-platform desktop window hosting the web UI
- Native application menus and system integration
- Platform-specific icons and behaviors
- Desktop notifications and app lifecycle management

**Future Plans**: Electron support is maintained for potential future desktop distribution scenarios. See `Electron.md` for detailed analysis of the Electron implementation.

## 📦 Deployment Modes

### 1. Windows Service (Recommended for Production)
- Runs automatically on system startup
- Managed through Windows Service Control Manager
- Suitable for server environments
- Background operation without user interface

### 2. Console Application
- Interactive command-line execution
- Suitable for development and testing
- Real-time log output
- Manual startup and shutdown

### 3. Docker Container
- `DockerDefaultTargetOS` configured for Linux
- Containerized deployment option
- Scalable cloud deployments

## 🔧 Dependencies & Requirements

### Runtime Requirements
- **.NET 8.0 Runtime** - Latest LTS version
- **Windows Service support** (for service installation mode)

### External Libraries
- **SmtpServer 10.0.1** - SMTP protocol handling
- **MimeKit** - Email message parsing
- **Serilog** - Structured logging framework
- **Autofac** - Dependency injection container

### Optional Dependencies
- **ElectronNET.API 23.6.2** - Desktop GUI support (when enabled)
- **Node.js & npm** - For building embedded Angular web UI

## 🔍 Troubleshooting

### Common Issues
1. **Port Conflicts**: Ensure SMTP port (default 25) is available
2. **File Permissions**: Service account needs write access to message directories
3. **Firewall**: Configure Windows Firewall to allow SMTP traffic

### Logging
Comprehensive logging is available through Serilog:
- Log files written to configured logging paths
- Structured JSON logging for analysis
- Multiple log levels (Information, Warning, Error, etc.)

## 📄 Related Documentation

- `Web/README.md` - Angular frontend documentation
- `Electron.md` - Electron desktop integration analysis
- `Web/TESTING.md` - Web UI testing guidelines

---

**License**: Apache License, Version 2.0
**Target Framework**: .NET 8.0
**Platform Support**: Windows (primary), Linux (Docker)
