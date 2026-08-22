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
import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, ListChecks, X, Plus, Pencil, Trash2, ChevronLeft } from 'lucide-angular';
import { RulesApiService } from '../../services/rules-api.service';
import { ToastNotificationService } from '../../services/toast-notification.service';
import {
  RuleDto,
  RuleType,
  RULE_TYPES,
  newRule,
  isRelayFamily,
  isForwardFamily,
  isConditionalFamily
} from '../../models/rule-dto';

/**
 * The desktop app's Rules window, web edition: manage the service's rule
 * set (forward, relay, conditional, invoke process, retention). Edits are
 * local until Save, which replaces the server's rule set wholesale —
 * matching the desktop's IPComm sync semantics.
 */
@Component({
  selector: 'app-rules-dialog',
  imports: [CommonModule, FormsModule, MatDialogModule, MatProgressSpinnerModule, MatTooltipModule, LucideAngularModule],
  template: `
    <div class="rules-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="icons.ListChecks" [size]="16"></lucide-icon>
        <h2 class="dialog-title">{{ editing ? (editingIsNew ? 'New Rule' : 'Edit Rule') : 'Rules' }}</h2>
        <button class="dialog-close" (click)="cancel()" [disabled]="isSaving">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>

      <!-- ======================= List view ======================= -->
      <div class="dialog-body" *ngIf="!isLoading && !editing">
        <div class="add-row">
          <select class="pc-input" [(ngModel)]="newRuleType" [disabled]="isSaving">
            <option *ngFor="let t of ruleTypes" [value]="t">{{ t }}</option>
          </select>
          <button class="pc-btn" (click)="addRule()" [disabled]="isSaving">
            <lucide-icon [img]="icons.Plus" [size]="14"></lucide-icon>
            <span>Add</span>
          </button>
        </div>

        <div class="rule-empty" *ngIf="rules.length === 0">
          No rules configured. Rules run automatically as messages arrive.
        </div>

        <div class="rule-row" *ngFor="let rule of rules; let i = index">
          <label class="pc-check rule-toggle" matTooltip="Enabled">
            <input type="checkbox" [(ngModel)]="rule.isEnabled" [disabled]="isSaving" (ngModelChange)="markDirty()" />
          </label>
          <div class="rule-info" (click)="editRule(i)">
            <div class="rule-name">{{ rule.name || rule.type }}</div>
            <div class="rule-desc">{{ summarize(rule) }}</div>
          </div>
          <button class="rule-action" (click)="editRule(i)" matTooltip="Edit" [disabled]="isSaving">
            <lucide-icon [img]="icons.Pencil" [size]="14"></lucide-icon>
          </button>
          <button class="rule-action rule-action-danger" (click)="deleteRule(i)" matTooltip="Delete" [disabled]="isSaving">
            <lucide-icon [img]="icons.Trash2" [size]="14"></lucide-icon>
          </button>
        </div>

        <div class="dialog-error" *ngIf="error">{{ error }}</div>
      </div>

      <!-- ======================= Edit view ======================= -->
      <div class="dialog-body" *ngIf="!isLoading && editing as rule">
        <div class="edit-type">{{ rule.type }}</div>

        <div class="field-grid">
          <label class="field-label" for="rule-name">Name</label>
          <input id="rule-name" class="pc-input" [(ngModel)]="rule.name" placeholder="(optional)" />

          <ng-container *ngIf="isRelayFamily(rule.type)">
            <label class="field-label" for="rule-server">Server</label>
            <div class="field-inline">
              <input id="rule-server" class="pc-input flex-1" [(ngModel)]="rule.smtpServer" placeholder="smtp.example.com" />
              <label class="field-label-inline">Port</label>
              <input class="pc-input w-20" type="number" [(ngModel)]="rule.smtpPort" min="1" max="65535" />
            </div>

            <span class="field-label"></span>
            <label class="pc-check">
              <input type="checkbox" [(ngModel)]="rule.smtpUseSSL" />
              <span>Use SSL</span>
            </label>

            <label class="field-label" for="rule-user">Username</label>
            <input id="rule-user" class="pc-input" [(ngModel)]="rule.smtpUsername" autocomplete="off" />

            <label class="field-label" for="rule-pass">Password</label>
            <input id="rule-pass" class="pc-input" type="password" [(ngModel)]="rule.smtpPassword" autocomplete="new-password" />

            <label class="field-label" for="rule-bcc">To BCC</label>
            <input id="rule-bcc" class="pc-input pc-mono" [(ngModel)]="rule.toBcc" placeholder="(optional)" />
          </ng-container>

          <ng-container *ngIf="isForwardFamily(rule.type)">
            <label class="field-label" for="rule-from">From</label>
            <input id="rule-from" class="pc-input pc-mono" [(ngModel)]="rule.fromEmail" placeholder="sender@example.com" />

            <label class="field-label" for="rule-to">To</label>
            <input id="rule-to" class="pc-input pc-mono" [(ngModel)]="rule.toEmail" placeholder="recipient@example.com" />
          </ng-container>

          <ng-container *ngIf="isConditionalFamily(rule.type)">
            <label class="field-label" for="rule-rxh">Header Rx</label>
            <input id="rule-rxh" class="pc-input pc-mono" [(ngModel)]="rule.regexHeaderMatch" placeholder="regex matched against headers" />

            <label class="field-label" for="rule-rxb">Body Rx</label>
            <input id="rule-rxb" class="pc-input pc-mono" [(ngModel)]="rule.regexBodyMatch" placeholder="regex matched against body" />
          </ng-container>

          <ng-container *ngIf="rule.type === 'Conditional Forward with Retry'">
            <label class="field-label" for="rule-retries">Retries</label>
            <div class="field-inline">
              <input id="rule-retries" class="pc-input w-20" type="number" [(ngModel)]="rule.retryAttempts" min="1" />
              <label class="field-label-inline">Delay (s)</label>
              <input class="pc-input w-20" type="number" [(ngModel)]="rule.retryAttemptDelaySeconds" min="1" />
            </div>
          </ng-container>

          <ng-container *ngIf="rule.type === 'Invoke Process'">
            <label class="field-label" for="rule-proc">Process</label>
            <input id="rule-proc" class="pc-input pc-mono" [(ngModel)]="rule.processToRun" placeholder="C:\\path\\to\\program.exe" />

            <label class="field-label" for="rule-args">Arguments</label>
            <input id="rule-args" class="pc-input pc-mono" [(ngModel)]="rule.processCommandLine" placeholder="%e expands to the message file" />
          </ng-container>

          <ng-container *ngIf="rule.type === 'Cleanup Mail'">
            <label class="field-label" for="rule-days">Keep (days)</label>
            <input id="rule-days" class="pc-input w-20" type="number" [(ngModel)]="rule.mailRetentionDays" min="1" />
          </ng-container>

          <span class="field-label"></span>
          <label class="pc-check">
            <input type="checkbox" [(ngModel)]="rule.isEnabled" />
            <span>Enabled</span>
          </label>
        </div>
      </div>

      <div class="dialog-loading" *ngIf="isLoading">
        <mat-spinner diameter="28" strokeWidth="3"></mat-spinner>
      </div>

      <!-- ======================= Actions ======================= -->
      <div class="dialog-actions" *ngIf="!editing">
        <span class="dirty-note" *ngIf="isDirty">Unsaved changes</span>
        <button class="pc-btn" (click)="cancel()" [disabled]="isSaving">Cancel</button>
        <button class="pc-btn pc-btn-primary" (click)="saveAll()" [disabled]="isSaving || isLoading || !isDirty">
          <mat-spinner *ngIf="isSaving" diameter="14" strokeWidth="2"></mat-spinner>
          <span>{{ isSaving ? 'Saving…' : 'Save' }}</span>
        </button>
      </div>

      <div class="dialog-actions" *ngIf="editing">
        <button class="pc-btn" (click)="closeEditor(false)">
          <lucide-icon [img]="icons.ChevronLeft" [size]="14"></lucide-icon>
          <span>Back</span>
        </button>
        <span class="flex-spacer"></span>
        <button class="pc-btn pc-btn-primary" (click)="closeEditor(true)">OK</button>
      </div>
    </div>
  `,
  styles: [`
    .rules-dialog {
      width: 520px;
      max-width: 90vw;
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
      padding: 12px 18px 4px;
      border-top: 1px solid var(--pc-border-soft);
      max-height: 62vh;
      overflow-y: auto;
    }

    .dialog-loading {
      display: flex;
      justify-content: center;
      padding: 40px;
      border-top: 1px solid var(--pc-border-soft);
    }

    .add-row {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }

    .add-row .pc-input { flex: 1; }

    .rule-empty {
      padding: 22px 0;
      text-align: center;
      font-size: 12.5px;
      color: var(--pc-muted);
    }

    .rule-row {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 10px;
      border: 1px solid var(--pc-border);
      border-radius: 8px;
      margin-bottom: 8px;
      background: var(--pc-surface);
      transition: border-color 0.12s ease;
    }

    .rule-row:hover { border-color: var(--pc-accent); }

    .rule-toggle { flex-shrink: 0; }

    .rule-info {
      flex: 1;
      min-width: 0;
      cursor: pointer;
    }

    .rule-name {
      font-size: 13px;
      font-weight: 600;
      color: var(--pc-ink-strong);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .rule-desc {
      font-size: 11.5px;
      color: var(--pc-muted);
      font-family: var(--pc-font-mono);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .rule-action {
      display: inline-flex;
      padding: 6px;
      border-radius: 6px;
      border: 1px solid transparent;
      background: transparent;
      color: var(--pc-muted);
      cursor: pointer;
      flex-shrink: 0;
    }

    .rule-action:hover { background: var(--pc-hover); color: var(--pc-accent-text); }
    .rule-action-danger:hover { background: var(--pc-danger-soft); color: var(--pc-danger-strong); }

    .edit-type {
      display: inline-block;
      margin-bottom: 12px;
      padding: 2px 10px;
      border-radius: 999px;
      background: var(--pc-accent-soft);
      color: var(--pc-accent-text);
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
    }

    .field-grid {
      display: grid;
      grid-template-columns: 82px 1fr;
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
    .w-20 { width: 80px; }
    .flex-1 { flex: 1; }
    .flex-spacer { flex: 1; }

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

    .dirty-note {
      align-self: center;
      font-size: 11.5px;
      color: var(--pc-warn);
      margin-right: auto;
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
export class RulesDialogComponent {
  protected readonly icons = { ListChecks, X, Plus, Pencil, Trash2, ChevronLeft };
  protected readonly ruleTypes = RULE_TYPES;
  protected readonly isRelayFamily = isRelayFamily;
  protected readonly isForwardFamily = isForwardFamily;
  protected readonly isConditionalFamily = isConditionalFamily;

  rules: RuleDto[] = [];
  isLoading = true;
  isSaving = false;
  isDirty = false;
  error: string | null = null;

  newRuleType: RuleType = 'Forward';

  editing: RuleDto | null = null;
  editingIsNew = false;
  private editingIndex = -1;

  constructor(
    private dialogRef: MatDialogRef<RulesDialogComponent, boolean>,
    private rulesApi: RulesApiService,
    private toastService: ToastNotificationService
  ) {
    this.rulesApi.getRules().subscribe({
      next: rules => {
        this.rules = rules;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load rules.';
        this.isLoading = false;
      }
    });
  }

  markDirty(): void {
    this.isDirty = true;
  }

  summarize(rule: RuleDto): string {
    switch (rule.type) {
      case 'Forward':
        return `${rule.type}: ${rule.smtpServer || '?'} → ${rule.toEmail || '?'}`;
      case 'Relay':
        return `${rule.type}: via ${rule.smtpServer || '?'}:${rule.smtpPort || 25}`;
      case 'Conditional Forward':
      case 'Conditional Forward with Retry':
        return `${rule.type}: ${rule.regexHeaderMatch || rule.regexBodyMatch || '(no condition)'} → ${rule.toEmail || '?'}`;
      case 'Invoke Process':
        return `${rule.type}: ${rule.processToRun || '?'}`;
      case 'Cleanup Mail':
        return `${rule.type}: keep ${rule.mailRetentionDays ?? '?'} day(s)`;
      default:
        return rule.type;
    }
  }

  addRule(): void {
    this.editing = newRule(this.newRuleType);
    this.editingIsNew = true;
    this.editingIndex = -1;
  }

  editRule(index: number): void {
    // Edit a copy so Back discards changes
    this.editing = { ...this.rules[index] };
    this.editingIsNew = false;
    this.editingIndex = index;
  }

  closeEditor(apply: boolean): void {
    if (apply && this.editing) {
      if (this.editingIsNew) {
        this.rules.push(this.editing);
      } else if (this.editingIndex >= 0) {
        this.rules[this.editingIndex] = this.editing;
      }
      this.isDirty = true;
    }

    this.editing = null;
    this.editingIsNew = false;
    this.editingIndex = -1;
  }

  deleteRule(index: number): void {
    this.rules.splice(index, 1);
    this.isDirty = true;
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  saveAll(): void {
    this.error = null;
    this.isSaving = true;

    this.rulesApi.updateRules(this.rules).subscribe({
      next: saved => {
        this.rules = saved;
        this.isDirty = false;
        this.isSaving = false;
        this.toastService.showSuccess(`Saved ${saved.length} rule(s)`);
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.isSaving = false;
        this.error = err?.error?.error || err?.message || 'Failed to save rules.';
      }
    });
  }
}
