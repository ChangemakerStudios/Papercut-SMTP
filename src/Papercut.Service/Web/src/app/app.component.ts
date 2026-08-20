import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { forkJoin } from 'rxjs';
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
        [selectedMessageCount]="((messageState.selectedIds$ | async) ?? []).length"
        [canForward]="!!(messageState.currentMessageId$ | async)"
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
    const ids = this.messageState.getSelectedIds();
    if (ids.length === 0) return;

    // one request per message: the api has no bulk delete, and the desktop
    // deletes them one at a time too
    forkJoin(ids.map(id => this.messageApiService.deleteMessage(id))).subscribe({
      next: () => {
        // The list owns the selection, and it is the only thing that knows
        // which message sat next to these -- let it decide where to go
        this.messageState.notifyDeleted(ids);
      },
      error: (err) => {
        this.loggingService.error('Failed to delete messages', err);
        this.toastService.showError(ids.length === 1 ? 'Failed to delete message' : 'Failed to delete messages');
        // some may have gone through; resync rather than trust the local list
        this.messageState.requestRefresh();
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
