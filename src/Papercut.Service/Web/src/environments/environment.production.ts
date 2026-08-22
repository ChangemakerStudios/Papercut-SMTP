// Production environment configuration
// Used for production builds (ng build --configuration production)

export const environment = {
  production: true,
  name: 'production',
  apiBaseUrl: '/api',
  signalRUrl: '/hubs/messages',
  enableLogging: false,
  logLevel: 'error',
  enableNotifications: true,
  enableAnalytics: true,
  cacheTimeout: 300000, // 5 minutes
  version: '7.0.0.0',
  buildTime: new Date().toISOString(),
  // Production optimizations
  enableServiceWorker: true,
  compressionEnabled: true,
  sslRequired: false // Set to true if HTTPS is required
};
