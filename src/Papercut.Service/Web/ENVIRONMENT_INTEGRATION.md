# Environment Integration Guide

This document explains how the Papercut Angular application now uses the EnvironmentService across all major services.

## Services Updated to Use Environment Configuration

### 1. MessageRepository
- **Before**: Hardcoded `/api/messages` URL
- **After**: Uses `environmentService.getApiEndpoint('messages')`
- **Benefit**: Different API URLs for dev/staging/production

### 2. SignalRService
- **Before**: Hardcoded `/hubs/messages` URL and `LogLevel.Information`
- **After**: Uses `environmentService.getSignalRUrl()` and environment-based log levels
- **Benefit**: Dynamic SignalR URLs and logging based on environment

### 3. ToastNotificationService
- **Before**: Fixed 8-second notification duration
- **After**: Environment-based durations and notification enablement checks
- **Benefit**: Shorter notifications in production, longer in development

### 4. PlatformNotificationService
- **Before**: Always attempted notifications regardless of environment
- **After**: Checks `environmentService.areNotificationsEnabled`
- **Benefit**: Can disable notifications entirely in certain environments

### 5. LoggingService (New)
- **Purpose**: Centralized logging that respects environment log levels
- **Features**: Different log levels per environment, performance logging in dev
- **Benefit**: Production logs only errors, development logs everything

## Environment Configuration Examples

### Development Environment
```typescript
{
  production: false,
  name: 'development',
  apiBaseUrl: 'http://localhost:37408/api',
  signalRUrl: '/hubs/messages',
  enableLogging: true,
  logLevel: 'debug',
  enableNotifications: true,
  cacheTimeout: 10000 // Faster cache refresh for development
}
```

### Production Environment
```typescript
{
  production: true,
  name: 'production',
  apiBaseUrl: '/api',
  signalRUrl: '/hubs/messages',
  enableLogging: false,
  logLevel: 'error',
  enableNotifications: true,
  cacheTimeout: 300000 // Longer cache for performance
}
```

## How to Use in Your Services

### Basic Environment Access
```typescript
constructor(private environmentService: EnvironmentService) {
  if (this.environmentService.isProduction) {
    // Production-specific logic
  }
  
  const apiUrl = this.environmentService.getApiEndpoint('your-endpoint');
  const version = this.environmentService.version;
}
```

### Using the Logging Service
```typescript
constructor(private loggingService: LoggingService) {}

someMethod() {
  this.loggingService.info('Operation started');
  this.loggingService.debug('Debug info only in development');
  this.loggingService.error('This will always log');
}
```

### Performance Monitoring (Development Only)
```typescript
const start = performance.now();
// Your operation here
const duration = performance.now() - start;
this.loggingService.logPerformance('API Call', duration);
```

## Build Commands

### Development Build
```bash
npm run build:dev
# Uses environment.development.ts
# - Verbose logging
# - Longer notifications
# - Debug features enabled
```

### Production Build
```bash
npm run build:prod
# Uses environment.production.ts
# - Minimal logging
# - Optimized performance
# - Production URLs
```

### Staging Build
```bash
npm run build:staging
# Uses environment.staging.ts
# - Moderate logging
# - Production-like settings
# - Staging URLs
```

## Key Benefits

1. **Environment-Specific Behavior**: Different logging, notifications, and performance characteristics per environment
2. **Type Safety**: Full TypeScript support for environment variables
3. **Centralized Configuration**: All environment settings in one place
4. **Build Optimization**: Different builds for different deployment targets
5. **Developer Experience**: Enhanced logging and debugging in development
6. **Production Performance**: Optimized settings for production deployment

## Testing Different Environments

### Local Development
```bash
npm run start:dev
# Logs everything, longer notifications, debug features
```

### Staging Testing
```bash
npm run start:staging
# Production-like but with more logging for debugging
```

### Production Preview
```bash
npm run start:prod
# Minimal logging, optimized for performance
```

Each environment now has distinct behavior that's appropriate for its use case!
