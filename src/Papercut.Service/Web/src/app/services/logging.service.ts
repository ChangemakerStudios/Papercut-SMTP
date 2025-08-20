import { Injectable } from '@angular/core';
import { EnvironmentService } from './environment.service';

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
  NONE = 4
}

@Injectable({
  providedIn: 'root'
})
export class LoggingService {
  private currentLogLevel: LogLevel;

  constructor(private environmentService: EnvironmentService) {
    this.currentLogLevel = this.getLogLevelFromEnvironment();
  }

  private getLogLevelFromEnvironment(): LogLevel {
    if (!this.environmentService.isLoggingEnabled) {
      return LogLevel.NONE;
    }

    switch (this.environmentService.logLevel.toLowerCase()) {
      case 'debug':
        return LogLevel.DEBUG;
      case 'info':
        return LogLevel.INFO;
      case 'warn':
      case 'warning':
        return LogLevel.WARN;
      case 'error':
        return LogLevel.ERROR;
      default:
        return this.environmentService.isProduction ? LogLevel.ERROR : LogLevel.INFO;
    }
  }

  debug(message: string, ...args: any[]): void {
    this.log(LogLevel.DEBUG, message, ...args);
  }

  info(message: string, ...args: any[]): void {
    this.log(LogLevel.INFO, message, ...args);
  }

  warn(message: string, ...args: any[]): void {
    this.log(LogLevel.WARN, message, ...args);
  }

  error(message: string, ...args: any[]): void {
    this.log(LogLevel.ERROR, message, ...args);
  }

  private log(level: LogLevel, message: string, ...args: any[]): void {
    if (level < this.currentLogLevel) {
      return;
    }

    const timestamp = new Date().toISOString();
    const levelName = LogLevel[level];
    const prefix = `[${timestamp}] [${levelName}] [Papercut]`;

    switch (level) {
      case LogLevel.DEBUG:
        console.debug(prefix, message, ...args);
        break;
      case LogLevel.INFO:
        console.log(prefix, message, ...args);
        break;
      case LogLevel.WARN:
        console.warn(prefix, message, ...args);
        break;
      case LogLevel.ERROR:
        console.error(prefix, message, ...args);
        break;
    }
  }

  /**
   * Log environment information (useful for debugging)
   */
  logEnvironmentInfo(): void {
    if (this.currentLogLevel <= LogLevel.INFO) {
      this.info('Environment Information', {
        name: this.environmentService.environment.name,
        production: this.environmentService.isProduction,
        version: this.environmentService.version,
        apiBaseUrl: this.environmentService.apiBaseUrl,
        signalRUrl: this.environmentService.signalRUrl,
        loggingEnabled: this.environmentService.isLoggingEnabled,
        logLevel: this.environmentService.logLevel,
        notificationsEnabled: this.environmentService.areNotificationsEnabled,
        buildTime: this.environmentService.buildTime
      });
    }
  }

  /**
   * Log performance metrics (development only)
   */
  logPerformance(operation: string, duration: number): void {
    if (this.environmentService.isDevelopment && this.currentLogLevel <= LogLevel.DEBUG) {
      this.debug(`Performance: ${operation} took ${duration}ms`);
    }
  }

  /**
   * Log API calls (development only)
   */
  logApiCall(method: string, url: string, status?: number, duration?: number): void {
    if (this.environmentService.isDevelopment && this.currentLogLevel <= LogLevel.DEBUG) {
      const statusText = status ? ` (${status})` : '';
      const durationText = duration ? ` - ${duration}ms` : '';
      this.debug(`API: ${method} ${url}${statusText}${durationText}`);
    }
  }
}
