import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { RefDto } from '../models/ref-dto';
import { EnvironmentService } from './environment.service';
import { LoggingService } from './logging.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private _isConnected = new BehaviorSubject<boolean>(false);
  private _newMessage = new BehaviorSubject<RefDto | null>(null);
  private _messageListChanged = new BehaviorSubject<boolean>(false);

  public isConnected$ = this._isConnected.asObservable();
  public newMessage$ = this._newMessage.asObservable();
  public messageListChanged$ = this._messageListChanged.asObservable();

  constructor(
    private environmentService: EnvironmentService,
    private loggingService: LoggingService
  ) {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    // Build the SignalR connection using environment configuration
    const hubUrl = this.environmentService.getSignalRUrl();
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Configure for cross-origin if needed
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(this.getSignalRLogLevel())
      .build();

    // Set up event handlers
    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle connection state changes
    this.connection.onclose((error: Error | undefined) => {
      this.loggingService.warn('SignalR connection closed', error);
      this._isConnected.next(false);
    });

    this.connection.onreconnecting((error: Error | undefined) => {
      this.loggingService.info('SignalR reconnecting', error);
      this._isConnected.next(false);
    });

    this.connection.onreconnected((connectionId: string | undefined) => {
      this.loggingService.info('SignalR reconnected', { connectionId });
      this._isConnected.next(true);
      this.joinMessagesGroup();
    });

    // Handle incoming messages - return void to prevent response expectation
    this.connection.on('NewMessageReceived', (message: RefDto): void => {
      try {
        this.loggingService.debug('New message received via SignalR', message);
        this._newMessage.next(message);
      } catch (error) {
        this.loggingService.error('Error handling new message via SignalR', error);
      }
    });

    this.connection.on('MessageListChanged', (): void => {
      try {
        this.loggingService.debug('Message list changed via SignalR');
        this._messageListChanged.next(true);
      } catch (error) {
        this.loggingService.error('Error handling message list change via SignalR', error);
      }
    });
  }

  public async start(): Promise<void> {
    if (!this.connection) {
      this.initializeConnection();
    }

    try {
      if (this.connection?.state === signalR.HubConnectionState.Disconnected) {
        await this.connection.start();
        this.loggingService.info('SignalR connection started successfully');
        this._isConnected.next(true);
        
        // Wait a bit before joining the group to ensure connection is stable
        await new Promise(resolve => setTimeout(resolve, 100));
        await this.joinMessagesGroup();
      }
    } catch (error) {
      this.loggingService.error('Failed to start SignalR connection', error);
      this._isConnected.next(false);
      
      // Retry connection after a delay, but limit retries
      setTimeout(() => this.start(), 5000);
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      try {
        // Only try to leave group if we're connected
        if (this.connection.state === signalR.HubConnectionState.Connected) {
          await this.leaveMessagesGroup();
        }
        await this.connection.stop();
        this.loggingService.info('SignalR connection stopped');
      } catch (error) {
        this.loggingService.error('Error stopping SignalR connection', error);
      } finally {
        this._isConnected.next(false);
      }
    }
  }

  private async joinMessagesGroup(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinMessagesGroup');
        this.loggingService.debug('Joined Messages group');
      } catch (error) {
        this.loggingService.error('Failed to join Messages group', error);
        // Don't throw here, just log the error
      }
    } else {
      this.loggingService.warn('Cannot join Messages group - connection not ready', { 
        state: this.connection?.state 
      });
    }
  }

  private async leaveMessagesGroup(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveMessagesGroup');
        this.loggingService.debug('Left Messages group');
      } catch (error) {
        this.loggingService.error('Failed to leave Messages group', error);
        // Don't throw here, just log the error
      }
    }
  }

  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  public async ensureConnection(): Promise<boolean> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return true;
    }

    if (this.connection?.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.start();
        return this.isConnected();
      } catch (error) {
        this.loggingService.error('Failed to ensure SignalR connection', error);
        return false;
      }
    }

    // If in any other state (Connecting, Reconnecting, etc.), wait a bit and check again
    await new Promise(resolve => setTimeout(resolve, 1000));
    return this.isConnected();
  }

  private getSignalRLogLevel(): signalR.LogLevel {
    if (!this.environmentService.isLoggingEnabled) {
      return signalR.LogLevel.None;
    }

    switch (this.environmentService.logLevel) {
      case 'debug':
        return signalR.LogLevel.Debug;
      case 'info':
        return signalR.LogLevel.Information;
      case 'warn':
      case 'warning':
        return signalR.LogLevel.Warning;
      case 'error':
        return signalR.LogLevel.Error;
      default:
        return this.environmentService.isProduction 
          ? signalR.LogLevel.Warning 
          : signalR.LogLevel.Information;
    }
  }
}
