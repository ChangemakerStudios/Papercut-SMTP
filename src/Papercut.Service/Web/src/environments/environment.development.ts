// Development environment configuration
// Used when specifically targeting development environment

export const environment = {
  production: false,
  name: 'development',
  apiBaseUrl: 'http://localhost:37408/api',
  signalRUrl: '/hubs/messages',
  enableLogging: true,
  logLevel: 'debug',
  enableNotifications: true,
  enableAnalytics: false,
  cacheTimeout: 10000, // 10 seconds for faster development
  version: '7.0.0.0-dev',
  buildTime: new Date().toISOString(),
  // Development-specific settings
  mockData: false,
  debugMode: true,
  hotReload: true
};
