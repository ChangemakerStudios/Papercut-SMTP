import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NavigationComponent } from './components/navigation/navigation.component';
import { BottomToolbarComponent } from './components/bottom-toolbar/bottom-toolbar.component';
import { NotificationPermissionComponent } from './components/notification-permission/notification-permission.component';
import { ThemeService } from './services/theme.service';
import { EnvironmentService } from './services/environment.service';
import { LoggingService } from './services/logging.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, MatSnackBarModule, NavigationComponent, BottomToolbarComponent, NotificationPermissionComponent],
  template: `
    <div class="app-container">
      <app-navigation></app-navigation>
      <app-notification-permission></app-notification-permission>
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
      <app-bottom-toolbar 
        [selectedMessageCount]="selectedMessageCount"
        [totalMessageCount]="totalMessageCount"
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
  selectedMessageCount = 0;
  totalMessageCount = 0;

  constructor(
    private themeService: ThemeService,
    private environmentService: EnvironmentService,
    private loggingService: LoggingService
  ) {
    // Initialize theme service
    // Log environment info using the logging service
    this.loggingService.logEnvironmentInfo();
    this.loggingService.info('Papercut application started');
  }

  onForward(): void {
    // TODO: Implement forward functionality
    this.loggingService.debug('Forward clicked from toolbar');
  }

  onDeleteSelected(): void {
    // TODO: Implement delete selected functionality
    this.loggingService.debug('Delete selected clicked from toolbar');
  }

  onDeleteAll(): void {
    // TODO: Implement delete all functionality
    this.loggingService.debug('Delete all clicked from toolbar');
  }
} 