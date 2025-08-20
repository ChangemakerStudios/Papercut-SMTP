import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { Observable } from 'rxjs';
import { PlatformNotificationService } from '../../services/platform-notification.service';

@Component({
  selector: 'app-notification-permission',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <!-- Background overlay for better separation -->
    <div *ngIf="shouldShowPrompt" 
         class="fixed inset-0 bg-black/10 dark:bg-black/30 z-40 backdrop-blur-[1px]"></div>
    
    <!-- Notification card -->
    <div *ngIf="shouldShowPrompt" 
         class="fixed top-8 left-1/2 transform -translate-x-1/2 z-50 max-w-md w-full mx-4 md:mx-0">
      <mat-card class="shadow-2xl dark:shadow-black/50 rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-900 ring-1 ring-black/5 dark:ring-white/10">
        <!-- Header section with icon and text -->
        <div class="flex items-start gap-4 p-4 pb-3">
          <div class="flex-shrink-0 w-12 h-12 bg-blue-50 dark:bg-blue-900/20 rounded-full flex items-center justify-center">
            <mat-icon class="text-blue-600 dark:text-blue-400">notifications</mat-icon>
          </div>
          <div class="flex-1 min-w-0">
            <h3 class="text-base font-medium text-gray-900 dark:text-white mb-1">
              Enable Notifications
            </h3>
            <p class="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
              Get notified when new emails arrive, even when this tab isn't active.
            </p>
          </div>
        </div>
        
        <!-- Button section at bottom -->
        <div class="px-4 pb-4 flex justify-end gap-2">
          <button mat-button 
                  (click)="dismissPermissionPrompt()" 
                  class="text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 text-sm">
            Not Now
          </button>
          <button mat-raised-button 
                  color="primary" 
                  (click)="requestPermission()"
                  class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg text-sm font-medium">
            Enable Notifications
          </button>
        </div>
      </mat-card>
    </div>
  `,
  styles: []
})
export class NotificationPermissionComponent implements OnInit {
  shouldShowPrompt = false;
  permissionStatus$: Observable<NotificationPermission>;
  isSupported$: Observable<boolean>;

  constructor(private notificationService: PlatformNotificationService) {
    this.permissionStatus$ = this.notificationService.permissionStatus$;
    this.isSupported$ = this.notificationService.isSupported$;
  }

  ngOnInit(): void {
    // Check if we should show the permission prompt
    this.checkShouldShowPrompt();
    
    // Subscribe to permission status changes
    this.permissionStatus$.subscribe(status => {
      this.updatePromptVisibility(status);
    });
  }

  private checkShouldShowPrompt(): void {
    const isSupported = this.notificationService.isSupported();
    const permissionStatus = this.notificationService.getPermissionStatus();
    const hasBeenDismissed = localStorage.getItem('notification-permission-dismissed') === 'true';
    
    // Show prompt if:
    // - Notifications are supported
    // - Permission is still 'default' (not granted or denied)
    // - User hasn't dismissed it before
    this.shouldShowPrompt = isSupported && permissionStatus === 'default' && !hasBeenDismissed;
  }

  private updatePromptVisibility(status: NotificationPermission): void {
    const hasBeenDismissed = localStorage.getItem('notification-permission-dismissed') === 'true';
    const isSupported = this.notificationService.isSupported();
    
    this.shouldShowPrompt = isSupported && status === 'default' && !hasBeenDismissed;
  }

  async requestPermission(): Promise<void> {
    try {
      const permission = await this.notificationService.requestPermission();
      
      if (permission === 'granted') {
        this.shouldShowPrompt = false;
        
        // Show a test notification
        setTimeout(() => {
          this.notificationService.showNotification({
            title: 'Notifications Enabled!',
            body: 'You\'ll now receive notifications for new emails.',
            tag: 'permission-granted'
          });
        }, 500);
      }
    } catch (error) {
      // Error handled by PlatformNotificationService
    }
  }

  dismissPermissionPrompt(): void {
    this.shouldShowPrompt = false;
    
    // Remember that the user dismissed this
    localStorage.setItem('notification-permission-dismissed', 'true');
  }
}
