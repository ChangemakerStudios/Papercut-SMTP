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

import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { LucideAngularModule, AlertTriangle, HelpCircle, X } from 'lucide-angular';

export interface ConfirmDialogData {
  title: string;
  /** The question itself. Keep it to one line. */
  message: string;
  /** Optional second line for a consequence worth spelling out. */
  detail?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Paints the confirm button red and swaps in the warning icon. */
  danger?: boolean;
  /**
   * Which button takes focus. Defaults to the confirm button, since the user
   * asked for this action -- pass 'cancel' for anything that cannot be undone,
   * so a reflexive Enter does not do the damage.
   */
  initialFocus?: 'confirm' | 'cancel';
}

/**
 * The app's confirmation dialog, replacing the browser's native confirm().
 * That one is unstyleable, ignores the theme, and carries a "Don't allow
 * localhost to prompt you again" checkbox that can silently disable
 * confirmation for the whole app.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, LucideAngularModule],
  template: `
    <div class="confirm-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="data.danger ? icons.AlertTriangle : icons.HelpCircle"
          [size]="16"
        [class.icon-danger]="data.danger"></lucide-icon>
        <h2 class="dialog-title">{{ data.title }}</h2>
        <button class="dialog-close" (click)="cancel()" aria-label="Close">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>
    
      <div class="dialog-body">
        <p class="confirm-message">{{ data.message }}</p>
        @if (data.detail) {
          <p class="confirm-detail">{{ data.detail }}</p>
        }
      </div>
    
      <div class="dialog-actions">
        <button class="pc-btn"
          [attr.cdkFocusInitial]="focusCancel ? '' : null"
          (click)="cancel()">
          {{ data.cancelLabel || 'Cancel' }}
        </button>
        <button class="pc-btn"
          [class.pc-btn-primary]="!data.danger"
          [class.pc-btn-danger]="data.danger"
          [attr.cdkFocusInitial]="focusCancel ? null : ''"
          (click)="confirm()">
          {{ data.confirmLabel || 'OK' }}
        </button>
      </div>
    </div>
    `,
  styles: [`
    .confirm-dialog {
      min-width: 360px;
      max-width: 440px;
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

    .icon-danger { color: var(--pc-danger); }

    .dialog-title {
      font-size: var(--pc-text-title);
      font-weight: 700;
      color: var(--pc-ink-strong);
      margin: 0 auto 0 0;
    }

    .dialog-close {
      display: inline-flex;
      padding: 5px;
      border-radius: 6px;
      border: 1px solid transparent;
      background: transparent;
      color: var(--pc-muted);
      cursor: pointer;
    }

    .dialog-close:hover { background: var(--pc-hover); color: var(--pc-ink); }

    .dialog-body {
      padding: 4px 18px 4px;
    }

    .confirm-message {
      margin: 0;
      font-size: var(--pc-text-ui);
      line-height: var(--pc-leading-ui);
      color: var(--pc-ink);
    }

    .confirm-detail {
      margin: 8px 0 0;
      font-size: var(--pc-text-small);
      line-height: var(--pc-leading-ui);
      color: var(--pc-muted);
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 16px 18px 16px;
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

    .pc-btn:hover { background: var(--pc-hover); }

    .pc-btn:focus-visible {
      outline: 2px solid var(--pc-accent);
      outline-offset: 2px;
    }

    .pc-btn-primary {
      background: var(--pc-accent);
      border-color: var(--pc-accent);
      color: var(--pc-on-chrome);
    }

    .pc-btn-primary:hover {
      background: color-mix(in srgb, var(--pc-accent) 85%, #000);
    }

    .pc-btn-danger {
      background: var(--pc-danger);
      border-color: var(--pc-danger);
      color: var(--pc-on-chrome);
    }

    .pc-btn-danger:hover {
      background: color-mix(in srgb, var(--pc-danger) 85%, #000);
    }

    .pc-btn-danger:focus-visible {
      outline-color: var(--pc-danger);
    }
  `]
})
export class ConfirmDialogComponent {
  protected readonly icons = { AlertTriangle, HelpCircle, X };

  constructor(
    private dialogRef: MatDialogRef<ConfirmDialogComponent, boolean>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) {}

  get focusCancel(): boolean {
    return this.data.initialFocus === 'cancel';
  }

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
