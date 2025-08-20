import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarRef, MatSnackBarConfig } from '@angular/material/snack-bar';
import { ComponentType } from '@angular/cdk/overlay';
import { Component, Inject } from '@angular/core';
import { MAT_SNACK_BAR_DATA, MatSnackBarAction } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { EnvironmentService } from './environment.service';

export interface ToastData {
  message: string;
  action?: string;
  icon?: string;
  type?: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
  clickable?: boolean;
}

@Component({
  selector: 'app-toast-notification',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  template: `
    <div class="flex items-center gap-3 py-1">
      <mat-icon *ngIf="data.icon" [ngClass]="getIconClass()">{{ data.icon }}</mat-icon>
      <span class="flex-1 text-sm">{{ data.message }}</span>
      <button *ngIf="data.action" 
              mat-button 
              mat-snack-bar-action
              class="!text-current !min-w-0">
        {{ data.action }}
      </button>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ToastNotificationComponent {
  constructor(@Inject(MAT_SNACK_BAR_DATA) public data: ToastData) {}

  getIconClass(): string {
    const baseClass = 'text-lg';
    switch (this.data.type) {
      case 'success': return `${baseClass} text-green-500`;
      case 'error': return `${baseClass} text-red-500`;
      case 'warning': return `${baseClass} text-yellow-500`;
      case 'info': return `${baseClass} text-blue-500`;
      default: return baseClass;
    }
  }
}

@Injectable({
  providedIn: 'root'
})
export class ToastNotificationService {
  constructor(
    private snackBar: MatSnackBar,
    private environmentService: EnvironmentService
  ) {}

  showNewMessageToast(subject: string, sender: string, messageId: string, onMessageClick: () => void): MatSnackBarRef<ToastNotificationComponent> | null {
    // Check if notifications are enabled in environment
    if (!this.environmentService.areNotificationsEnabled) {
      return null;
    }

    const data: ToastData = {
      message: `New message from ${sender}: ${subject}`,
      action: 'VIEW',
      icon: 'email',
      type: 'info',
      duration: this.getNotificationDuration(),
      clickable: true
    };

    const config: MatSnackBarConfig = {
      duration: data.duration,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [
        'toast-notification',
        'toast-new-message',
        'bg-blue-600',
        'text-white',
        '!max-w-md'
      ],
      data
    };

    const snackBarRef = this.snackBar.openFromComponent(ToastNotificationComponent, config);

    // Handle click events
    snackBarRef.onAction().subscribe(() => {
      onMessageClick();
      snackBarRef.dismiss();
    });

    // Make the entire toast clickable
    snackBarRef.containerInstance.snackBarConfig.panelClass = [
      ...snackBarRef.containerInstance.snackBarConfig.panelClass || [],
      'cursor-pointer'
    ];

    return snackBarRef;
  }

  showSuccess(message: string, action?: string): MatSnackBarRef<ToastNotificationComponent> {
    return this.showToast({
      message,
      action,
      icon: 'check_circle',
      type: 'success',
      duration: 4000
    });
  }

  showError(message: string, action?: string): MatSnackBarRef<ToastNotificationComponent> {
    return this.showToast({
      message,
      action,
      icon: 'error',
      type: 'error',
      duration: 6000
    });
  }

  showWarning(message: string, action?: string): MatSnackBarRef<ToastNotificationComponent> {
    return this.showToast({
      message,
      action,
      icon: 'warning',
      type: 'warning',
      duration: 5000
    });
  }

  showInfo(message: string, action?: string): MatSnackBarRef<ToastNotificationComponent> {
    return this.showToast({
      message,
      action,
      icon: 'info',
      type: 'info',
      duration: 4000
    });
  }

  private showToast(data: ToastData): MatSnackBarRef<ToastNotificationComponent> {
    const config: MatSnackBarConfig = {
      duration: data.duration,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [
        'toast-notification',
        `toast-${data.type}`,
        this.getBackgroundClass(data.type),
        'text-white',
        '!max-w-md'
      ],
      data
    };

    return this.snackBar.openFromComponent(ToastNotificationComponent, config);
  }

  private getBackgroundClass(type: ToastData['type']): string {
    switch (type) {
      case 'success': return 'bg-green-600';
      case 'error': return 'bg-red-600';
      case 'warning': return 'bg-yellow-600';
      case 'info': return 'bg-blue-600';
      default: return 'bg-gray-600';
    }
  }

  private getNotificationDuration(): number {
    // Base durations in milliseconds
    const baseDuration = 8000; // 8 seconds for new messages
    
    // Adjust based on environment
    if (this.environmentService.isProduction) {
      return baseDuration * 0.75; // Shorter in production
    } else if (this.environmentService.isDevelopment) {
      return baseDuration * 1.5; // Longer in development for testing
    }
    
    return baseDuration;
  }
}
