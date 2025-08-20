// This file contains the default environment configuration
// Used for development builds (ng serve)

export const environment = {
  production: false,
  name: 'development',
  apiBaseUrl: 'http://localhost:37408/api',
  signalRUrl: '/hubs/messages',
  enableLogging: true,
  logLevel: 'debug',
  enableNotifications: true,
  enableAnalytics: false,
  cacheTimeout: 30000, // 30 seconds
  version: '7.0.0.0',
  buildTime: new Date().toISOString()
};
