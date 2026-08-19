import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '../../services/theme.service';
import { LoggingService } from '../../services/logging.service';
import { McpService, McpStatus } from '../../services/mcp.service';
import { Observable, map, switchMap, of } from 'rxjs';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatToolbarModule, 
    MatButtonModule, 
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <nav class="papercut-navbar">
      <div class="nav-container">
        <!-- Brand Section -->
        <div class="logo-section">
          <div class="logo-container" routerLink="/">
            <img [src]="(isDarkTheme$ | async) ? '/assets/images/papercut-logo-dark.png' : '/assets/images/papercut-logo-light.png'" 
                 alt="Papercut Logo" 
                 class="papercut-logo">
          </div>
        </div>
        
        <!-- Navigation Actions (Desktop Layout Style) -->
        <div class="nav-actions">
          <button mat-stroked-button class="papercut-nav-btn" (click)="showLog()">
            <mat-icon>list_alt</mat-icon>
            <span>Log</span>
          </button>
          
          <button mat-stroked-button class="papercut-nav-btn" (click)="showRules()">
            <mat-icon>rule</mat-icon>
            <span>Rules</span>
          </button>
          
          <button mat-stroked-button class="papercut-nav-btn" (click)="showOptions()">
            <mat-icon>settings</mat-icon>
            <span>Options</span>
          </button>

          <button mat-stroked-button
                  *ngIf="(mcpStatus$ | async)?.enabled"
                  class="papercut-nav-btn"
                  (click)="copyMcpUrl()"
                  [matTooltip]="'MCP endpoint: ' + ((mcpStatus$ | async)?.url || '') + ' (click to copy)'">
            <mat-icon>bolt</mat-icon>
            <span>{{ mcpCopied ? 'Copied!' : 'MCP' }}</span>
          </button>

          <!-- Theme Toggle integrated into buttons -->
          <button mat-stroked-button 
                  (click)="toggleTheme()"
                  matTooltip="{{ (isDarkTheme$ | async) ? 'Switch to Light Theme' : 'Switch to Dark Theme' }}"
                  class="papercut-nav-btn">
            <mat-icon>{{ (isDarkTheme$ | async) ? 'light_mode' : 'dark_mode' }}</mat-icon>
            <span>Theme</span>
          </button>
        </div>
      </div>
    </nav>
  `,
  styles: []
})
export class NavigationComponent implements OnDestroy {
  isDarkTheme$: Observable<boolean>;
  mcpStatus$: Observable<McpStatus>;
  mcpCopied = false;
  loadingTimeout: any;
  isLoadingMessage = false;
  private mcpUrl: string | null = null;
  private mcpCopiedTimeout: any;

  constructor(
    private themeService: ThemeService,
    private loggingService: LoggingService,
    private mcpService: McpService
  ) {
    this.isDarkTheme$ = this.themeService.theme$.pipe(
      map(theme => theme === 'dark')
    );
    this.mcpStatus$ = this.mcpService.status$;
    this.mcpStatus$.subscribe(status => this.mcpUrl = status.url);
  }

  copyMcpUrl(): void {
    if (!this.mcpUrl || !navigator.clipboard) {
      return;
    }

    navigator.clipboard.writeText(this.mcpUrl).then(() => {
      this.mcpCopied = true;
      clearTimeout(this.mcpCopiedTimeout);
      this.mcpCopiedTimeout = setTimeout(() => this.mcpCopied = false, 2000);
    });
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  showLog(): void {
    // TODO: Implement log functionality
    this.loggingService.debug('Show Log clicked');
  }

  showRules(): void {
    // TODO: Implement rules functionality
    this.loggingService.debug('Show Rules clicked');
  }

  showOptions(): void {
    // TODO: Implement options functionality
    this.loggingService.debug('Show Options clicked');
  }



  ngOnDestroy() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
    }
    if (this.mcpCopiedTimeout) {
      clearTimeout(this.mcpCopiedTimeout);
    }
  }
} 