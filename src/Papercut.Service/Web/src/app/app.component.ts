import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NavigationComponent } from './components/navigation/navigation.component';
import { BottomToolbarComponent } from './components/bottom-toolbar/bottom-toolbar.component';
import { NotificationPermissionComponent } from './components/notification-permission/notification-permission.component';
import { ForwardDialogComponent, ForwardDialogData } from './components/forward-dialog/forward-dialog.component';
import { ThemeService } from './services/theme.service';
import { EnvironmentService } from './services/environment.service';
import { LoggingService } from './services/logging.service';
import { MessageStateService } from './services/message-state.service';
import { MessageApiService } from './services/message-api.service';
import { ToastNotificationService } from './services/toast-notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, MatDialogModule, MatSnackBarModule, NavigationComponent, BottomToolbarComponent, NotificationPermissionComponent],
  template: `
    <div class="app-container">
      <app-navigation></app-navigation>
      <app-notification-permission></app-notification-permission>
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
      <app-bottom-toolbar
        [selectedMessageCount]="(messageState.currentMessageId$ | async) ? 1 : 0"
        [totalMessageCount]="(messageState.totalCount$ | async) ?? 0"
        (forward)="onForward()"
        (deleteSelected)="onDeleteSelected()"
        (deleteAll)="onDeleteAll()">
      </app-bottom-toolbar>
    </div>
  `,
  styles: []
})
export class AppComponent {
  title = 'Papercut';

  constructor(
    public messageState: MessageStateService,
    private themeService: ThemeService,
    private environmentService: EnvironmentService,
    private loggingService: LoggingService,
    private messageApiService: MessageApiService,
    private toastService: ToastNotificationService,
    private dialog: MatDialog,
    private router: Router
  ) {
    this.loggingService.logEnvironmentInfo();
    this.loggingService.info('Papercut application started');
  }

  onForward(): void {
    const messageId = this.messageState.getCurrentMessageId();
    if (!messageId) return;

    const data: ForwardDialogData = { messageId, subject: null };

    this.dialog.open<ForwardDialogComponent, ForwardDialogData, boolean>(ForwardDialogComponent, {
      data,
      autoFocus: '#fwd-server',
      disableClose: true
    }).afterClosed().subscribe(sent => {
      if (sent) {
        this.toastService.showSuccess('Message forwarded');
      }
    });
  }

  onDeleteSelected(): void {
    const messageId = this.messageState.getCurrentMessageId();
    if (!messageId) return;

    this.messageApiService.deleteMessage(messageId).subscribe({
      next: () => {
        this.router.navigate(['/'], { queryParamsHandling: 'preserve' }).then(() => {
          this.messageState.requestRefresh();
        });
      },
      error: (err) => {
        this.loggingService.error('Failed to delete message', err);
        this.toastService.showError('Failed to delete message');
      }
    });
  }

  onDeleteAll(): void {
    this.messageApiService.deleteAllMessages().subscribe({
      next: () => {
        this.router.navigate(['/'], { queryParamsHandling: 'preserve' }).then(() => {
          this.messageState.requestRefresh();
        });
      },
      error: (err) => {
        this.loggingService.error('Failed to delete all messages', err);
        this.toastService.showError('Failed to delete messages');
      }
    });
  }
}
