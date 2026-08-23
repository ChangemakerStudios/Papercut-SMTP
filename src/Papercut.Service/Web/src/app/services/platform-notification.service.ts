import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { EnvironmentService } from './environment.service';
import { LoggingService } from './logging.service';

export interface PlatformNotificationOptions {
  title: string;
  body?: string;
  icon?: string;
  badge?: string;
  tag?: string;
  requireInteraction?: boolean;
  silent?: boolean;
  onClick?: () => void;
}

@Injectable({
  providedIn: 'root'
})
export class PlatformNotificationService {
  private _permissionStatus = new BehaviorSubject<NotificationPermission>('default');
  private _isSupported = new BehaviorSubject<boolean>(false);

  public permissionStatus$ = this._permissionStatus.asObservable();
  public isSupported$ = this._isSupported.asObservable();

  constructor(
    private environmentService: EnvironmentService,
    private loggingService: LoggingService
  ) {
    this.checkSupport();
    this.updatePermissionStatus();
  }

  private checkSupport(): void {
    const isSupported = 'Notification' in window && typeof Notification !== 'undefined';
    this._isSupported.next(isSupported);
    
    if (isSupported) {
      this.loggingService.info('Platform notifications are supported');
    } else {
      this.loggingService.warn('Platform notifications are not supported in this browser');
    }
  }

  private updatePermissionStatus(): void {
    if (this._isSupported.value) {
      this._permissionStatus.next(Notification.permission);
    }
  }

  public async requestPermission(): Promise<NotificationPermission> {
    if (!this._isSupported.value) {
      this.loggingService.warn('Platform notifications are not supported');
      return 'denied';
    }

    try {
      const permission = await Notification.requestPermission();
      this._permissionStatus.next(permission);
      
      if (permission === 'granted') {
        this.loggingService.info('Notification permission granted');
      } else if (permission === 'denied') {
        this.loggingService.warn('Notification permission denied');
      } else {
        this.loggingService.info('Notification permission dismissed');
      }
      
      return permission;
    } catch (error) {
      this.loggingService.error('Error requesting notification permission', error);
      this._permissionStatus.next('denied');
      return 'denied';
    }
  }

  public async showNotification(options: PlatformNotificationOptions): Promise<boolean> {
    if (!this._isSupported.value) {
      this.loggingService.warn('Platform notifications are not supported');
      return false;
    }

    // Check if we have permission
    if (Notification.permission !== 'granted') {
      this.loggingService.warn('Notification permission not granted');
      return false;
    }

    try {
      const notification = new Notification(options.title, {
        body: options.body,
        icon: options.icon || this.getDefaultIcon(),
        badge: options.badge || this.getDefaultBadge(),
        tag: options.tag,
        requireInteraction: options.requireInteraction || false,
        silent: options.silent || false
      });

      // Handle click event
      if (options.onClick) {
        notification.onclick = (event) => {
          event.preventDefault();
          window.focus(); // Focus the browser window
          options.onClick!();
          notification.close();
        };
      } else {
        // Default behavior: focus the window when clicked
        notification.onclick = (event) => {
          event.preventDefault();
          window.focus();
          notification.close();
        };
      }

      // Auto-close after a reasonable time if not requiring interaction
      if (!options.requireInteraction) {
        setTimeout(() => {
          notification.close();
        }, 8000); // 8 seconds
      }

      this.loggingService.debug('Platform notification shown', { title: options.title });
      return true;
    } catch (error) {
      this.loggingService.error('Error showing platform notification', error);
      return false;
    }
  }

  public async showNewMessageNotification(subject: string, sender: string, onClickCallback?: () => void): Promise<boolean> {
    const title = `New Message: ${subject}`;
    const body = `From: ${sender}`;
    
    return this.showNotification({
      title,
      body,
      icon: this.getEmailIcon(),
      tag: 'new-message', // This will replace any existing new-message notification
      requireInteraction: false,
      onClick: () => {
        if (onClickCallback) {
          onClickCallback();
        }
      }
    });
  }

  private getDefaultIcon(): string {
    // Use the Papercut icon from the public assets
    return '/icons/Papercut-icon.png';
  }

  private getDefaultBadge(): string {
    // Badge is typically smaller and monochrome
    return '/icons/Papercut-icon.png';
  }

  private getEmailIcon(): string {
    // For email notifications, we could use a specific email icon
    // For now, use the default Papercut icon
    return this.getDefaultIcon();
  }

  public getPermissionStatus(): NotificationPermission {
    return this._permissionStatus.value;
  }

  public isSupported(): boolean {
    return this._isSupported.value;
  }

  public isPermissionGranted(): boolean {
    return this.isSupported() && this.getPermissionStatus() === 'granted';
  }

  public async ensurePermission(): Promise<boolean> {
    if (!this.isSupported()) {
      return false;
    }

    if (this.getPermissionStatus() === 'granted') {
      return true;
    }

    if (this.getPermissionStatus() === 'default') {
      const permission = await this.requestPermission();
      return permission === 'granted';
    }

    return false;
  }

  /**
   * Check if the browser tab is currently visible/focused
   */
  public isTabVisible(): boolean {
    return !document.hidden && document.hasFocus();
  }

  /**
   * Show notification only if the tab is not visible (user is not actively looking at the app)
   */
  public async showNotificationIfTabHidden(options: PlatformNotificationOptions): Promise<boolean> {
    if (this.isTabVisible()) {
      this.loggingService.debug('Tab is visible, skipping platform notification');
      return false;
    }

    return this.showNotification(options);
  }

  /**
   * Show new message notification only if the tab is not visible
   */
  public async showNewMessageNotificationIfTabHidden(subject: string, sender: string, onClickCallback?: () => void): Promise<boolean> {
    // Check if notifications are enabled in environment
    if (!this.environmentService.areNotificationsEnabled) {
      this.loggingService.debug('Notifications disabled in environment, skipping platform notification');
      return false;
    }

    if (this.isTabVisible()) {
      this.loggingService.debug('Tab is visible, skipping platform notification for new message');
      return false;
    }

    return this.showNewMessageNotification(subject, sender, onClickCallback);
  }
}
