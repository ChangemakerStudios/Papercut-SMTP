// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LucideAngularModule, Settings2, X, Check } from 'lucide-angular';
import { SettingsApiService, ServerSettings } from '../../services/settings-api.service';
import { UserSettingsService, MessageSortOrder } from '../../services/user-settings.service';
import { ThemeService, ThemePreference, AccentColor } from '../../services/theme.service';
import { ToastNotificationService } from '../../services/toast-notification.service';

/**
 * The desktop app's Options window, web edition. Server settings (SMTP
 * binding, MCP) persist via the settings API; viewer settings (sort order,
 * theme, accent, notifications) persist locally.
 */
@Component({
  selector: 'app-options-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatProgressSpinnerModule, LucideAngularModule],
  template: `
    <div class="options-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="icons.Settings2" [size]="16"></lucide-icon>
        <h2 class="dialog-title">Options</h2>
        <button class="dialog-close" (click)="cancel()" [disabled]="isSaving">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>

      <div class="dialog-body">
        <!-- Server section -->
        <div class="section-title">SMTP Server</div>
        <div class="field-grid">
          <label class="field-label" for="opt-ip">IP Address</label>
          <select id="opt-ip" class="pc-input" [(ngModel)]="smtpIP" [disabled]="isSaving || isLoading">
            <option *ngFor="let ip of availableIPs" [value]="ip">{{ ip }}</option>
          </select>

          <label class="field-label" for="opt-port">Port</label>
          <div class="field-inline">
            <input id="opt-port" class="pc-input w-24" type="number" [(ngModel)]="smtpPort"
                   min="1" max="65535" [disabled]="isSaving || isLoading" />
            <span class="field-hint">default is 25 (2525 in Docker)</span>
          </div>
        </div>

        <!-- Messages section -->
        <div class="section-title">Messages</div>
        <div class="field-grid">
          <label class="field-label" for="opt-sort">Sort Order</label>
          <select id="opt-sort" class="pc-input" [(ngModel)]="sortOrder" [disabled]="isSaving">
            <option value="desc">Descending (newest first)</option>
            <option value="asc">Ascending (oldest first)</option>
          </select>

          <label class="field-label">Notify</label>
          <label class="pc-check">
            <input type="checkbox" [(ngModel)]="notificationsEnabled" [disabled]="isSaving" />
            <span>Show new message notifications</span>
          </label>
        </div>

        <!-- Appearance section -->
        <div class="section-title">Appearance</div>
        <div class="field-grid">
          <label class="field-label" for="opt-theme">Theme</label>
          <select id="opt-theme" class="pc-input" [(ngModel)]="themePreference" [disabled]="isSaving">
            <option value="system">System</option>
            <option value="light">Light</option>
            <option value="dark">Dark</option>
          </select>

          <label class="field-label">Accent</label>
          <div class="accent-grid">
            <button *ngFor="let accent of themeService.accentColors"
                    class="accent-swatch-btn"
                    [class.selected]="accent.name === selectedAccent.name"
                    [style.background]="accent.value"
                    [title]="accent.name"
                    [disabled]="isSaving"
                    (click)="selectedAccent = accent">
              <lucide-icon *ngIf="accent.name === selectedAccent.name" [img]="icons.Check" [size]="13"></lucide-icon>
            </button>
          </div>
        </div>

        <!-- MCP section -->
        <div class="section-title">Integrations</div>
        <div class="field-grid">
          <label class="field-label">MCP</label>
          <label class="pc-check">
            <input type="checkbox" [(ngModel)]="mcpEnabled" [disabled]="isSaving || isLoading" />
            <span>Enable the MCP server endpoint <span class="field-hint">(takes effect after restart)</span></span>
          </label>
        </div>

        <div class="dialog-error" *ngIf="error">{{ error }}</div>
      </div>

      <div class="dialog-actions">
        <button class="pc-btn" (click)="cancel()" [disabled]="isSaving">Cancel</button>
        <button class="pc-btn pc-btn-primary" (click)="save()" [disabled]="isSaving || isLoading">
          <mat-spinner *ngIf="isSaving" diameter="14" strokeWidth="2"></mat-spinner>
          <span>{{ isSaving ? 'Saving…' : 'Save' }}</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .options-dialog {
      min-width: 440px;
      max-width: 540px;
      background: var(--pc-surface);
      color: var(--pc-ink);
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 14px 18px 10px;
      color: var(--pc-accent-text);
    }

    .dialog-title {
      flex: 1;
      font-size: 15px;
      font-weight: 700;
      color: var(--pc-ink-strong);
      margin: 0;
    }

    .dialog-close {
      display: inline-flex;
      padding: 4px;
      border-radius: 6px;
      border: none;
      background: transparent;
      color: var(--pc-faint);
      cursor: pointer;
    }

    .dialog-close:hover { background: var(--pc-hover); color: var(--pc-ink); }

    .dialog-body {
      padding: 4px 18px;
      border-top: 1px solid var(--pc-border-soft);
      max-height: 65vh;
      overflow-y: auto;
    }

    .section-title {
      margin: 14px 0 8px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--pc-accent-text);
    }

    .field-grid {
      display: grid;
      grid-template-columns: 86px 1fr;
      gap: 10px 12px;
      align-items: center;
    }

    .field-label {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--pc-faint);
      text-align: right;
    }

    .field-inline { display: flex; align-items: center; gap: 10px; }

    .field-hint {
      font-size: 11px;
      color: var(--pc-faint);
      text-transform: none;
      letter-spacing: normal;
      font-weight: 400;
    }

    .pc-input {
      width: 100%;
      min-width: 0;
      height: 32px;
      padding: 0 10px;
      font-size: 13px;
      font-family: inherit;
      color: var(--pc-ink);
      background: var(--pc-surface);
      border: 1px solid var(--pc-border);
      border-radius: 6px;
      outline: none;
      transition: border-color 0.12s ease, box-shadow 0.12s ease;
    }

    .pc-input:focus {
      border-color: var(--pc-accent);
      box-shadow: 0 0 0 2px var(--pc-accent-soft);
    }

    .w-24 { width: 90px; }

    .pc-check {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      font-size: 12.5px;
      color: var(--pc-ink);
      cursor: pointer;
      user-select: none;
    }

    .pc-check input { accent-color: var(--pc-accent); }

    .accent-grid {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }

    .accent-swatch-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border-radius: 6px;
      border: 2px solid transparent;
      cursor: pointer;
      color: #fff;
      transition: transform 0.1s ease;
    }

    .accent-swatch-btn:hover { transform: scale(1.12); }

    .accent-swatch-btn.selected {
      border-color: var(--pc-ink-strong);
    }

    .dialog-error {
      margin: 12px 0 4px;
      padding: 8px 12px;
      border-radius: 6px;
      border: 1px solid var(--pc-danger);
      background: var(--pc-danger-soft);
      color: var(--pc-danger-strong);
      font-size: 12.5px;
      word-break: break-word;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 14px 18px 16px;
    }

    .pc-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      height: 32px;
      padding: 0 16px;
      font-size: 12.5px;
      font-weight: 600;
      color: var(--pc-ink);
      background: var(--pc-surface);
      border: 1px solid var(--pc-border);
      border-radius: 6px;
      cursor: pointer;
      transition: background-color 0.12s ease;
    }

    .pc-btn:hover:not(:disabled) { background: var(--pc-hover); }
    .pc-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .pc-btn-primary {
      background: var(--pc-accent);
      border-color: var(--pc-accent);
      color: var(--pc-on-chrome);
    }

    .pc-btn-primary:hover:not(:disabled) {
      background: color-mix(in srgb, var(--pc-accent) 85%, #000);
    }
  `]
})
export class OptionsDialogComponent {
  protected readonly icons = { Settings2, X, Check };

  isLoading = true;
  isSaving = false;
  error: string | null = null;

  // Server settings
  smtpIP = 'Any';
  smtpPort = 25;
  mcpEnabled = false;
  availableIPs: string[] = ['Any'];
  private original: ServerSettings | null = null;

  // Viewer settings
  sortOrder: MessageSortOrder;
  notificationsEnabled: boolean;
  themePreference: ThemePreference;
  selectedAccent: AccentColor;

  constructor(
    private dialogRef: MatDialogRef<OptionsDialogComponent, boolean>,
    private settingsApi: SettingsApiService,
    private userSettings: UserSettingsService,
    public themeService: ThemeService,
    private toastService: ToastNotificationService
  ) {
    this.sortOrder = this.userSettings.getSortOrder();
    this.notificationsEnabled = this.userSettings.areNotificationsEnabled();
    this.themePreference = this.themeService.getCurrentPreference();
    this.selectedAccent = this.themeService.getCurrentAccent();

    this.settingsApi.getSettings().subscribe({
      next: settings => {
        this.original = settings;
        this.smtpIP = settings.smtpIP;
        this.smtpPort = settings.smtpPort;
        this.mcpEnabled = settings.mcpEnabled;
        this.availableIPs = settings.availableIPs.includes(settings.smtpIP)
          ? settings.availableIPs
          : [settings.smtpIP, ...settings.availableIPs];
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load server settings.';
        this.isLoading = false;
      }
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  save(): void {
    this.error = null;

    if (!this.smtpPort || this.smtpPort < 1 || this.smtpPort > 65535) {
      this.error = 'SMTP port must be between 1 and 65535.';
      return;
    }

    // Viewer settings apply immediately
    this.userSettings.setSortOrder(this.sortOrder);
    this.userSettings.setNotificationsEnabled(this.notificationsEnabled);
    this.themeService.setPreference(this.themePreference);
    this.themeService.setAccent(this.selectedAccent);

    // Server settings only round-trip when something changed
    const smtpChanged = this.original !== null
      && (this.smtpIP !== this.original.smtpIP || this.smtpPort !== this.original.smtpPort);
    const mcpChanged = this.original !== null && this.mcpEnabled !== this.original.mcpEnabled;

    if (!smtpChanged && !mcpChanged) {
      this.dialogRef.close(true);
      return;
    }

    this.isSaving = true;

    this.settingsApi.updateSettings({
      smtpIP: smtpChanged ? this.smtpIP : undefined,
      smtpPort: smtpChanged ? this.smtpPort : undefined,
      mcpEnabled: mcpChanged ? this.mcpEnabled : undefined
    }).subscribe({
      next: result => {
        if (result.smtpRebound) {
          this.toastService.showSuccess(`SMTP server now listening on ${this.smtpIP}:${this.smtpPort}`);
        }
        if (result.mcpRequiresRestart) {
          this.toastService.showInfo('MCP setting saved — restart the service to apply');
        }
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSaving = false;
        this.error = err?.error?.error || err?.message || 'Failed to save settings.';
      }
    });
  }
}
