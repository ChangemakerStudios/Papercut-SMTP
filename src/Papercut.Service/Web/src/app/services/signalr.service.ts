import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { RefDto } from '../models/ref-dto';

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

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    // Build the SignalR connection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/messages', {
        // Configure for cross-origin if needed
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up event handlers
    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle connection state changes
    this.connection.onclose((error: Error | undefined) => {
      console.warn('SignalR connection closed:', error);
      this._isConnected.next(false);
    });

    this.connection.onreconnecting((error: Error | undefined) => {
      console.log('SignalR reconnecting:', error);
      this._isConnected.next(false);
    });

    this.connection.onreconnected((connectionId: string | undefined) => {
      console.log('SignalR reconnected:', connectionId);
      this._isConnected.next(true);
      this.joinMessagesGroup();
    });

    // Handle incoming messages
    this.connection.on('NewMessageReceived', (message: RefDto) => {
      console.log('New message received via SignalR:', message);
      this._newMessage.next(message);
    });

    this.connection.on('MessageListChanged', () => {
      console.log('Message list changed via SignalR');
      this._messageListChanged.next(true);
    });
  }

  public async start(): Promise<void> {
    if (!this.connection) {
      this.initializeConnection();
    }

    try {
      if (this.connection?.state === signalR.HubConnectionState.Disconnected) {
        await this.connection.start();
        console.log('SignalR connection started successfully');
        this._isConnected.next(true);
        await this.joinMessagesGroup();
      }
    } catch (error) {
      console.error('Failed to start SignalR connection:', error);
      this._isConnected.next(false);
      
      // Retry connection after a delay
      setTimeout(() => this.start(), 5000);
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.leaveMessagesGroup();
        await this.connection.stop();
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      } finally {
        this._isConnected.next(false);
      }
    }
  }

  private async joinMessagesGroup(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinMessagesGroup');
        console.log('Joined Messages group');
      } catch (error) {
        console.error('Failed to join Messages group:', error);
      }
    }
  }

  private async leaveMessagesGroup(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveMessagesGroup');
        console.log('Left Messages group');
      } catch (error) {
        console.error('Failed to leave Messages group:', error);
      }
    }
  }

  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}
