// Staging environment configuration
// Used for staging/testing builds

export const environment = {
  production: false,
  name: 'staging',
  apiBaseUrl: '/api',
  signalRUrl: '/hubs/messages',
  enableLogging: true,
  logLevel: 'info',
  enableNotifications: true,
  enableAnalytics: false,
  cacheTimeout: 60000, // 1 minute
  version: '7.0.0.0-staging',
  buildTime: new Date().toISOString(),
  // Staging-specific settings
  mockData: false,
  debugMode: true,
  performanceMonitoring: true
};
