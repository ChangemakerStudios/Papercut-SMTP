import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  LucideAngularModule,
  Mail,
  ScrollText,
  ListChecks,
  Settings2,
  Zap,
  Sun,
  Moon,
  Palette,
  Check
} from 'lucide-angular';
import { ThemeService, AccentColor } from '../../services/theme.service';
import { LoggingService } from '../../services/logging.service';
import { McpService, McpStatus } from '../../services/mcp.service';
import { Observable, map } from 'rxjs';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatMenuModule,
    MatTooltipModule,
    LucideAngularModule
  ],
  template: `
    <nav class="papercut-navbar">
      <div class="nav-container">
        <!-- Brand -->
        <div class="logo-section">
          <div class="logo-container" routerLink="/">
            <span class="brand-logo-icon">
              <lucide-icon [img]="icons.Mail" [size]="22" [strokeWidth]="2"></lucide-icon>
            </span>
            <span class="brand-wordmark">
              Paper<span class="brand-cut">cut</span>
              <span class="brand-smtp">SMTP</span>
            </span>
          </div>
        </div>

        <!-- Actions -->
        <div class="nav-actions">
          <button class="papercut-nav-btn" (click)="showLog()">
            <lucide-icon [img]="icons.ScrollText" [size]="15"></lucide-icon>
            <span>Log</span>
          </button>

          <button class="papercut-nav-btn" (click)="showRules()">
            <lucide-icon [img]="icons.ListChecks" [size]="15"></lucide-icon>
            <span>Rules</span>
          </button>

          <button class="papercut-nav-btn" (click)="showOptions()">
            <lucide-icon [img]="icons.Settings2" [size]="15"></lucide-icon>
            <span>Options</span>
          </button>

          <button class="papercut-nav-btn"
                  *ngIf="(mcpStatus$ | async)?.enabled"
                  (click)="copyMcpUrl()"
                  [matTooltip]="'MCP endpoint: ' + ((mcpStatus$ | async)?.url || '') + ' (click to copy)'">
            <lucide-icon [img]="icons.Zap" [size]="15"></lucide-icon>
            <span>{{ mcpCopied ? 'Copied!' : 'MCP' }}</span>
          </button>

          <span class="nav-divider"></span>

          <!-- Theme accent picker -->
          <button class="papercut-nav-btn"
                  [matMenuTriggerFor]="accentMenu"
                  matTooltip="Theme accent">
            <lucide-icon [img]="icons.Palette" [size]="15"></lucide-icon>
          </button>
          <mat-menu #accentMenu="matMenu" class="accent-menu">
            <button mat-menu-item
                    *ngFor="let accent of themeService.accentColors"
                    (click)="setAccent(accent)"
                    class="accent-menu-item">
              <span class="accent-swatch" [style.background]="accent.value"></span>
              <span class="accent-name">{{ accent.name }}</span>
              <lucide-icon *ngIf="isCurrentAccent(accent)"
                           [img]="icons.Check" [size]="14"
                           class="accent-check"></lucide-icon>
            </button>
          </mat-menu>

          <!-- Theme toggle -->
          <button class="papercut-nav-btn"
                  (click)="toggleTheme()"
                  [matTooltip]="(isDarkTheme$ | async) ? 'Switch to light theme' : 'Switch to dark theme'">
            <lucide-icon [img]="(isDarkTheme$ | async) ? icons.Sun : icons.Moon" [size]="15"></lucide-icon>
          </button>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .nav-divider {
      width: 1px;
      height: 20px;
      margin: 0 6px;
      background: rgba(255, 255, 255, 0.2);
    }

    .accent-swatch {
      display: inline-block;
      width: 14px;
      height: 14px;
      border-radius: 4px;
      margin-right: 10px;
      flex-shrink: 0;
      border: 1px solid rgba(0, 0, 0, 0.15);
    }

    .accent-menu-item {
      display: flex !important;
      align-items: center;
    }

    .accent-name {
      flex: 1;
      font-size: 13px;
    }

    .accent-check {
      margin-left: 10px;
      color: var(--pc-accent-text);
    }
  `]
})
export class NavigationComponent implements OnDestroy {
  protected readonly icons = { Mail, ScrollText, ListChecks, Settings2, Zap, Sun, Moon, Palette, Check };

  isDarkTheme$: Observable<boolean>;
  mcpStatus$: Observable<McpStatus>;
  mcpCopied = false;
  private mcpUrl: string | null = null;
  private mcpCopiedTimeout: any;

  constructor(
    public themeService: ThemeService,
    private loggingService: LoggingService,
    private mcpService: McpService
  ) {
    this.isDarkTheme$ = this.themeService.theme$.pipe(
      map(theme => theme === 'dark')
    );
    this.mcpStatus$ = this.mcpService.status$;
    this.mcpStatus$.subscribe(status => this.mcpUrl = status.url);
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  setAccent(accent: AccentColor): void {
    this.themeService.setAccent(accent);
  }

  isCurrentAccent(accent: AccentColor): boolean {
    return this.themeService.getCurrentAccent().name === accent.name;
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
    if (this.mcpCopiedTimeout) {
      clearTimeout(this.mcpCopiedTimeout);
    }
  }
}
