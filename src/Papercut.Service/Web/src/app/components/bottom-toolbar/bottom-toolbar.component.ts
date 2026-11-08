import { Component, Input, Output, EventEmitter } from '@angular/core';

import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, Forward, Trash2, Trash } from 'lucide-angular';
import { ConfirmService } from '../../services/confirm.service';

@Component({
  selector: 'app-bottom-toolbar',
  imports: [MatTooltipModule, LucideAngularModule],
  template: `
    <div class="bottom-toolbar">
      <div class="toolbar-container">
        <!-- Left side - Action buttons -->
        <div class="toolbar-actions">
          <button class="toolbar-btn"
                  (click)="onForward()"
                  [disabled]="!canForward"
                  matTooltip="Forward the open message">
            <lucide-icon [img]="icons.Forward" [size]="14"></lucide-icon>
            <span>Forward</span>
          </button>

          <button class="toolbar-btn toolbar-btn-danger"
                  (click)="onDeleteSelected()"
                  [disabled]="!selectedMessageCount"
                  matTooltip="Delete selected message(s)">
            <lucide-icon [img]="icons.Trash2" [size]="14"></lucide-icon>
            <span>Delete{{ selectedMessageCount ? ' (' + selectedMessageCount + ')' : '' }}</span>
          </button>

          <button class="toolbar-btn toolbar-btn-danger"
                  (click)="onDeleteAll()"
                  [disabled]="!totalMessageCount"
                  matTooltip="Delete all messages">
            <lucide-icon [img]="icons.Trash" [size]="14"></lucide-icon>
            <span>Delete All</span>
          </button>
        </div>

        <!-- Right side - Status info -->
        <div class="toolbar-status">
          <span class="status-text">
            Papercut SMTP —
            <a href="https://github.com/ChangemakerStudios/Papercut-SMTP"
               target="_blank"
               class="status-link">github.com/ChangemakerStudios/Papercut-SMTP</a>
          </span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bottom-toolbar {
      background-color: var(--pc-surface-2);
      border-top: 1px solid var(--pc-border);
      padding: 8px 16px;
      min-height: 48px;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .toolbar-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
    }

    .toolbar-actions {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .toolbar-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 0 14px;
      height: 32px;
      font-size: 12px;
      font-weight: 600;
      letter-spacing: 0.01em;
      border-radius: 6px;
      border: 1px solid var(--pc-border);
      background-color: var(--pc-surface);
      color: var(--pc-ink);
      cursor: pointer;
      transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
    }

    .toolbar-btn:hover:not(:disabled) {
      background-color: var(--pc-hover);
      border-color: var(--pc-faint);
    }

    .toolbar-btn:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }

    .toolbar-btn-danger:hover:not(:disabled) {
      background-color: var(--pc-danger-soft);
      border-color: var(--pc-danger);
      color: var(--pc-danger-strong);
    }

    .toolbar-status {
      display: flex;
      align-items: center;
    }

    .status-text {
      font-size: 11px;
      color: var(--pc-muted);
    }

    .status-link {
      color: var(--pc-accent-text);
      text-decoration: none;
    }

    .status-link:hover {
      text-decoration: underline;
    }

    /* Mobile responsiveness */
    @media (max-width: 768px) {
      .toolbar-status {
        display: none;
      }

      .toolbar-actions {
        justify-content: center;
        width: 100%;
      }
    }

    @media (max-width: 480px) {
      .toolbar-btn span {
        display: none;
      }

      .toolbar-btn {
        padding: 0 10px;
        min-width: 36px;
        justify-content: center;
      }
    }
  `]
})
export class BottomToolbarComponent {
  protected readonly icons = { Forward, Trash2, Trash };

  @Input() selectedMessageCount = 0;
  @Input() totalMessageCount = 0;

  /** Forward acts on the one open message, not the whole ticked set. */
  @Input() canForward = false;

  @Output() forward = new EventEmitter<void>();
  @Output() deleteSelected = new EventEmitter<void>();
  @Output() deleteAll = new EventEmitter<void>();

  constructor(private confirmService: ConfirmService) {}

  onForward(): void {
    this.forward.emit();
  }

  onDeleteSelected(): void {
    if (this.selectedMessageCount === 0) return;

    const one = this.selectedMessageCount === 1;

    this.confirmService.confirm({
      title: one ? 'Delete Message' : 'Delete Messages',
      message: one
        ? 'Delete this message?'
        : `Delete these ${this.selectedMessageCount} messages?`,
      detail: 'The .eml file will be removed from the message folder.',
      confirmLabel: one ? 'Delete' : `Delete ${this.selectedMessageCount}`,
      danger: true
    }).subscribe(confirmed => {
      if (confirmed) this.deleteSelected.emit();
    });
  }

  onDeleteAll(): void {
    if (this.totalMessageCount === 0) return;

    this.confirmService.confirm({
      title: 'Delete All Messages',
      message: `Delete all ${this.totalMessageCount} messages?`,
      detail: 'Every .eml file in the message folder will be removed. This cannot be undone.',
      confirmLabel: 'Delete All',
      danger: true,
      // nothing here is recoverable -- do not let a reflexive Enter empty the inbox
      initialFocus: 'cancel'
    }).subscribe(confirmed => {
      if (confirmed) this.deleteAll.emit();
    });
  }
}
