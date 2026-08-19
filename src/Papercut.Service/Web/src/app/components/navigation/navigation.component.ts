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
  Monitor,
  Palette,
  Check,
  type LucideIconData
} from 'lucide-angular';
import { MatDialog } from '@angular/material/dialog';
import { ThemeService, AccentColor, ThemePreference } from '../../services/theme.service';
import { LoggingService } from '../../services/logging.service';
import { McpService, McpStatus } from '../../services/mcp.service';
import { OptionsDialogComponent } from '../options-dialog/options-dialog.component';
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

          <!-- Theme: System / Light / Dark -->
          <button class="papercut-nav-btn"
                  [matMenuTriggerFor]="themeMenu"
                  matTooltip="Theme">
            <lucide-icon [img]="currentThemeIcon()" [size]="15"></lucide-icon>
          </button>
          <mat-menu #themeMenu="matMenu">
            <button mat-menu-item
                    *ngFor="let option of themeOptions"
                    (click)="setThemePreference(option.value)"
                    class="theme-menu-item">
              <lucide-icon [img]="option.icon" [size]="15" class="theme-option-icon"></lucide-icon>
              <span class="theme-option-name">{{ option.label }}</span>
              <lucide-icon *ngIf="isCurrentPreference(option.value)"
                           [img]="icons.Check" [size]="14"
                           class="theme-check"></lucide-icon>
            </button>
          </mat-menu>
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

    .theme-menu-item {
      display: flex !important;
      align-items: center;
    }

    .theme-option-icon {
      margin-right: 10px;
      color: var(--pc-muted);
      display: inline-flex;
    }

    .theme-option-name {
      flex: 1;
      font-size: 13px;
    }

    .theme-check {
      margin-left: 10px;
      color: var(--pc-accent-text);
    }
  `]
})
export class NavigationComponent implements OnDestroy {
  protected readonly icons = { Mail, ScrollText, ListChecks, Settings2, Zap, Sun, Moon, Monitor, Palette, Check };

  protected readonly themeOptions: { value: ThemePreference; label: string; icon: LucideIconData }[] = [
    { value: 'system', label: 'System', icon: Monitor },
    { value: 'light', label: 'Light', icon: Sun },
    { value: 'dark', label: 'Dark', icon: Moon }
  ];

  isDarkTheme$: Observable<boolean>;
  mcpStatus$: Observable<McpStatus>;
  mcpCopied = false;
  private mcpUrl: string | null = null;
  private mcpCopiedTimeout: any;

  constructor(
    public themeService: ThemeService,
    private loggingService: LoggingService,
    private mcpService: McpService,
    private dialog: MatDialog
  ) {
    this.isDarkTheme$ = this.themeService.theme$.pipe(
      map(theme => theme === 'dark')
    );
    this.mcpStatus$ = this.mcpService.status$;
    this.mcpStatus$.subscribe(status => this.mcpUrl = status.url);
  }

  setThemePreference(preference: ThemePreference): void {
    this.themeService.setPreference(preference);
  }

  isCurrentPreference(preference: ThemePreference): boolean {
    return this.themeService.getCurrentPreference() === preference;
  }

  currentThemeIcon(): LucideIconData {
    const option = this.themeOptions.find(o => o.value === this.themeService.getCurrentPreference());
    return option?.icon ?? Monitor;
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
    this.dialog.open(OptionsDialogComponent, { autoFocus: false });
  }

  ngOnDestroy() {
    if (this.mcpCopiedTimeout) {
      clearTimeout(this.mcpCopiedTimeout);
    }
  }
}
