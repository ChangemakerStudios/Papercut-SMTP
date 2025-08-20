import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EnvironmentService {
  
  get environment() {
    return environment;
  }

  get isProduction(): boolean {
    return environment.production;
  }

  get isDevelopment(): boolean {
    return environment.name === 'development';
  }

  get isStaging(): boolean {
    return environment.name === 'staging';
  }

  get apiBaseUrl(): string {
    return environment.apiBaseUrl;
  }

  get signalRUrl(): string {
    return environment.signalRUrl;
  }

  get isLoggingEnabled(): boolean {
    return environment.enableLogging;
  }

  get logLevel(): string {
    return environment.logLevel;
  }

  get areNotificationsEnabled(): boolean {
    return environment.enableNotifications;
  }

  get version(): string {
    return environment.version;
  }

  get buildTime(): string {
    return environment.buildTime;
  }

  get cacheTimeout(): number {
    return environment.cacheTimeout;
  }

  /**
   * Log environment information to console (development only)
   * @deprecated Use LoggingService.logEnvironmentInfo() instead
   */
  logEnvironmentInfo(): void {
    // This method is deprecated - LoggingService handles environment logging now
    if (this.isDevelopment) {
      console.group('🌍 Environment Configuration');
      console.log('Environment:', environment.name);
      console.log('Production:', environment.production);
      console.log('API Base URL:', environment.apiBaseUrl);
      console.log('SignalR URL:', environment.signalRUrl);
      console.log('Logging Enabled:', environment.enableLogging);
      console.log('Log Level:', environment.logLevel);
      console.log('Notifications Enabled:', environment.enableNotifications);
      console.log('Version:', environment.version);
      console.log('Build Time:', environment.buildTime);
      console.log('Cache Timeout:', environment.cacheTimeout);
      console.groupEnd();
    }
  }

  /**
   * Get the full API endpoint URL
   */
  getApiEndpoint(path: string): string {
    const cleanPath = path.startsWith('/') ? path.slice(1) : path;
    return `${this.apiBaseUrl}/${cleanPath}`;
  }

  /**
   * Get the full SignalR hub URL
   */
  getSignalRUrl(): string {
    return this.signalRUrl;
  }
}
