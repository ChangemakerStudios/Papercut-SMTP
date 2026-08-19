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

import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LucideAngularModule, Forward, X } from 'lucide-angular';
import { MessageApiService } from '../../services/message-api.service';
import { ForwardMessageRequest } from '../../models';

export interface ForwardDialogData {
  messageId: string;
  subject: string | null;
}

interface SavedForwardSettings {
  server: string;
  port: number;
  useSsl: boolean;
  username: string;
  fromEmail: string;
  toEmail: string;
}

const FORWARD_SETTINGS_KEY = 'papercut-forward-settings';

// Same permissive check the desktop Forward dialog uses
const EMAIL_REGEX = /^([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})$/i;

/**
 * The desktop app's "Forward Message" dialog: re-deliver a captured message
 * to a real SMTP server with rewritten From/To. Last-used values are
 * remembered (never the password), matching desktop behavior.
 */
@Component({
  selector: 'app-forward-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatProgressSpinnerModule, LucideAngularModule],
  template: `
    <div class="forward-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="icons.Forward" [size]="16"></lucide-icon>
        <h2 class="dialog-title">Forward Message</h2>
        <button class="dialog-close" (click)="cancel()" [disabled]="isSending">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>

      <div class="dialog-subject" *ngIf="data.subject">{{ data.subject }}</div>

      <div class="dialog-body">
        <div class="field-grid">
          <label class="field-label" for="fwd-server">Server</label>
          <div class="field-inline">
            <input id="fwd-server" class="pc-input flex-1" [(ngModel)]="server"
                   placeholder="smtp.example.com" [disabled]="isSending" />
            <label class="field-label-inline" for="fwd-port">Port</label>
            <input id="fwd-port" class="pc-input w-20" type="number" [(ngModel)]="port"
                   min="1" max="65535" [disabled]="isSending" />
          </div>

          <span class="field-label"></span>
          <div class="field-inline field-checks">
            <label class="pc-check">
              <input type="checkbox" [(ngModel)]="useSsl" [disabled]="isSending" />
              <span>Use SSL</span>
            </label>
            <label class="pc-check">
              <input type="checkbox" [(ngModel)]="useAuthentication" [disabled]="isSending" />
              <span>Use Authentication</span>
            </label>
          </div>

          <ng-container *ngIf="useAuthentication">
            <label class="field-label" for="fwd-user">Username</label>
            <input id="fwd-user" class="pc-input" [(ngModel)]="username" [disabled]="isSending" autocomplete="off" />

            <label class="field-label" for="fwd-pass">Password</label>
            <input id="fwd-pass" class="pc-input" type="password" [(ngModel)]="password"
                   [disabled]="isSending" autocomplete="new-password" />
          </ng-container>

          <label class="field-label" for="fwd-from">From</label>
          <input id="fwd-from" class="pc-input pc-mono" [(ngModel)]="fromEmail"
                 placeholder="sender@example.com" [disabled]="isSending" />

          <label class="field-label" for="fwd-to">To</label>
          <input id="fwd-to" class="pc-input pc-mono" [(ngModel)]="toEmail"
                 placeholder="recipient@example.com" [disabled]="isSending" />
        </div>

        <div class="dialog-error" *ngIf="error">{{ error }}</div>
      </div>

      <div class="dialog-actions">
        <button class="pc-btn" (click)="cancel()" [disabled]="isSending">Cancel</button>
        <button class="pc-btn pc-btn-primary" (click)="send()" [disabled]="isSending">
          <mat-spinner *ngIf="isSending" diameter="14" strokeWidth="2"></mat-spinner>
          <span>{{ isSending ? 'Sending…' : 'Send' }}</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .forward-dialog {
      min-width: 420px;
      max-width: 520px;
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

    .dialog-subject {
      padding: 0 18px 8px;
      font-size: 12px;
      color: var(--pc-muted);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .dialog-body {
      padding: 8px 18px 4px;
      border-top: 1px solid var(--pc-border-soft);
    }

    .field-grid {
      display: grid;
      grid-template-columns: 76px 1fr;
      gap: 10px 12px;
      align-items: center;
    }

    .field-label, .field-label-inline {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--pc-faint);
      text-align: right;
    }

    .field-inline { display: flex; align-items: center; gap: 8px; min-width: 0; }
    .field-checks { gap: 18px; }

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

    .pc-input.pc-mono { font-family: var(--pc-font-mono); font-size: 12.5px; }
    .w-20 { width: 72px; }
    .flex-1 { flex: 1; }

    .pc-check {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 12.5px;
      color: var(--pc-ink);
      cursor: pointer;
      user-select: none;
    }

    .pc-check input { accent-color: var(--pc-accent); }

    .dialog-error {
      margin-top: 12px;
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
export class ForwardDialogComponent {
  protected readonly icons = { Forward, X };

  server = '';
  port = 25;
  useSsl = false;
  useAuthentication = false;
  username = '';
  password = '';
  fromEmail = '';
  toEmail = '';

  isSending = false;
  error: string | null = null;

  constructor(
    private dialogRef: MatDialogRef<ForwardDialogComponent, boolean>,
    @Inject(MAT_DIALOG_DATA) public data: ForwardDialogData,
    private messageApiService: MessageApiService
  ) {
    this.loadSavedSettings();
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  send(): void {
    this.error = this.validate();
    if (this.error) {
      return;
    }

    const request: ForwardMessageRequest = {
      server: this.server.trim(),
      port: this.port,
      useSsl: this.useSsl,
      username: this.useAuthentication ? this.username.trim() : null,
      password: this.useAuthentication ? this.password : null,
      fromEmail: this.fromEmail.trim(),
      toEmail: this.toEmail.trim()
    };

    this.isSending = true;

    this.messageApiService.forwardMessage(this.data.messageId, request).subscribe({
      next: () => {
        this.saveSettings();
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSending = false;
        this.error = err?.error?.detail || err?.error?.error || err?.message || 'Forward failed';
      }
    });
  }

  private validate(): string | null {
    if (!this.server.trim() || !this.fromEmail.trim() || !this.toEmail.trim()) {
      return 'Server, From, and To are required.';
    }

    if (!EMAIL_REGEX.test(this.fromEmail.trim()) || !EMAIL_REGEX.test(this.toEmail.trim())) {
      return 'From and To must be valid email addresses.';
    }

    if (!this.port || this.port < 1 || this.port > 65535) {
      return 'SMTP port must be between 1 and 65535.';
    }

    return null;
  }

  private loadSavedSettings(): void {
    try {
      const raw = localStorage.getItem(FORWARD_SETTINGS_KEY);
      if (!raw) return;

      const saved = JSON.parse(raw) as SavedForwardSettings;
      this.server = saved.server ?? '';
      this.port = saved.port ?? 25;
      this.useSsl = saved.useSsl ?? false;
      this.username = saved.username ?? '';
      this.useAuthentication = !!this.username;
      this.fromEmail = saved.fromEmail ?? '';
      this.toEmail = saved.toEmail ?? '';
    } catch {
      // Ignore corrupted saved settings
    }
  }

  private saveSettings(): void {
    // Password is intentionally not persisted, matching the desktop app
    const settings: SavedForwardSettings = {
      server: this.server.trim(),
      port: this.port,
      useSsl: this.useSsl,
      username: this.useAuthentication ? this.username.trim() : '',
      fromEmail: this.fromEmail.trim(),
      toEmail: this.toEmail.trim()
    };

    localStorage.setItem(FORWARD_SETTINGS_KEY, JSON.stringify(settings));
  }
}
