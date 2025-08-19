# Papercut Web UI

A modern Angular 17 web interface for the Papercut SMTP service, providing an intuitive way to view, manage, and interact with email messages during development and testing.

## 🎯 Purpose & Functionality

Papercut Web UI is the frontend application for Papercut SMTP, an email testing and development tool. It provides:

- **Email Management**: View and manage SMTP messages received by the Papercut service
- **Message Browsing**: Browse email lists with pagination, search, and filtering
- **Content Viewing**: View detailed email content, headers, and attachments
- **Message Operations**: Forward, delete, and manage emails
- **File Downloads**: Download raw message files for analysis

## 🏗️ Architecture & Modern Angular Features

### Standalone Components Architecture
- **Angular 17**: Latest major version with modern features
- **Standalone Components**: No NgModules - components are self-contained
- **Bootstrap Pattern**: Uses `bootstrapApplication()` for modern initialization
- **Functional Providers**: Modern routing and service configuration

### Key Angular 17 Features
- Standalone components and directives
- Modern routing with functional providers
- Built-in HTTP client and animations
- TypeScript 5.3+ support
- Improved performance and tree-shaking

## 🛠️ Technology Stack

### Frontend Framework
- **Angular 17.2.0** - Latest major version
- **Angular Material 17.3.10** - Material Design components
- **Angular CDK** - Component development kit
- **RxJS 7.8** - Reactive programming

### Styling & UI
- **Tailwind CSS 3.4.17** - Utility-first CSS framework
- **SCSS** - Advanced CSS preprocessing
- **Angular Material** - Pre-built UI components

### Build & Development
- **Angular CLI 17.2.0** - Development tools
- **Karma + Jasmine** - Testing framework
- **PostCSS + Autoprefixer** - CSS processing

## 📁 Application Structure

### Core Components
- **Navigation** - Top navigation bar
- **Message List** - Main email inbox view with pagination
- **Message Detail** - Individual email viewer
- **Bottom Toolbar** - Action buttons (forward, delete)
- **Resizer** - Adjustable panel widths
- **Pagination** - Page navigation controls

### Services Layer
- **MessageService** - Core email operations
- **MessageRepository** - Data access abstraction
- **ThemeService** - Dark/light theme management
- **EmailService** - Email-specific operations

### Data Models
- **RefDto** - Lightweight message references for lists
- **DetailDto** - Full message details
- **EmailAddressDto** - Sender/recipient information
- **EmailSectionDto** - Message body and attachments

## 🔌 Backend Integration

### API Communication
- RESTful API calls to `/api/messages` endpoints
- Proxy configuration routes API calls to `localhost:37408`
- Integrates with the C# Papercut.Service backend
- Uses HTTP interceptors for request/response handling

### Data Flow
- Angular services communicate with C# Web API
- Models match C# DTOs exactly for seamless integration
- Real-time message updates and pagination
- File download capabilities for raw messages

## ✨ Key Features

### Email Management
- Virtual scrolling for large message lists
- Pagination with configurable page sizes
- Message selection and bulk operations
- Real-time message loading

### User Experience
- Responsive design with Tailwind CSS
- Dark/light theme support
- Resizable panels for customization
- Loading states and error handling

### Performance
- Standalone components for better tree-shaking
- Virtual scrolling for large datasets
- Lazy loading of message details
- Efficient change detection

## 🚀 Development Setup

### Prerequisites
- Node.js (version 18 or higher)
- npm or yarn package manager
- Angular CLI 17.x

### Installation
```bash
# Navigate to the Web directory
cd src/Papercut.Service/Web

# Install dependencies
npm install
```

### Development Commands
```bash
# Start development server with proxy
npm start

# Build for production
npm run build

# Build with watch mode
npm run watch

# Run unit tests
npm test
```

### Configuration
- **Proxy Setup**: API calls are proxied to `localhost:37408` for local development
- **Environment**: Environment-specific configurations in `src/environments/`
- **Styling**: SCSS compilation with PostCSS and Tailwind CSS
- **Build**: Optimized production builds with Angular CLI

## 🌐 Development Server

The development server runs on `http://localhost:4200` by default and includes:

- **Hot Reload**: Automatic browser refresh on file changes
- **Proxy Configuration**: API calls routed to backend service
- **Source Maps**: Debug-friendly development experience
- **Live Reload**: Real-time updates during development

## 🧪 Testing

The project includes a comprehensive testing setup:

- **Karma**: Test runner for unit tests
- **Jasmine**: Testing framework
- **Coverage**: Code coverage reporting
- **Component Testing**: Angular component testing utilities

## 📦 Build & Deployment

### Production Build
```bash
npm run build
```

The build process:
- Compiles TypeScript to JavaScript
- Processes SCSS to CSS
- Optimizes and bundles assets
- Generates production-ready files in `dist/papercut-web/`

### Build Configuration
- **Optimization**: Production builds are optimized for performance
- **Tree Shaking**: Unused code is eliminated
- **Bundle Analysis**: Size budgets configured for performance monitoring
- **Source Maps**: Optional source maps for debugging

## 🔧 Customization

### Styling
- **Tailwind CSS**: Utility-first CSS framework with custom configuration
- **SCSS**: Advanced CSS preprocessing capabilities
- **Themes**: Dark/light theme support with ThemeService
- **Responsive**: Mobile-first responsive design

### Components
- **Standalone**: All components are standalone for easy customization
- **Material Design**: Angular Material components for consistent UI
- **Custom Components**: Resizable panels, custom pagination, etc.

## 📚 Additional Resources

- [Angular Documentation](https://angular.io/docs)
- [Angular Material](https://material.angular.io/)
- [Tailwind CSS](https://tailwindcss.com/)
- [RxJS](https://rxjs.dev/)

## 🤝 Contributing

When contributing to the Web UI:

1. Follow Angular style guide conventions
2. Use standalone components
3. Write unit tests for new features
4. Ensure responsive design compatibility
5. Test with different screen sizes and themes

## 📄 License

This project is licensed under the Apache License, Version 2.0. See the LICENSE file for details.

---

**Note**: This Web UI is designed to work seamlessly with the C# Papercut.Service backend. Ensure the backend service is running on the configured port (default: 37408) for full functionality.
