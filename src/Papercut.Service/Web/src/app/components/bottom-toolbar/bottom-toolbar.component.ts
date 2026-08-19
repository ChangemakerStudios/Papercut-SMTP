import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, Forward, Trash2, Trash } from 'lucide-angular';

@Component({
  selector: 'app-bottom-toolbar',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, LucideAngularModule],
  template: `
    <div class="bottom-toolbar">
      <div class="toolbar-container">
        <!-- Left side - Action buttons -->
        <div class="toolbar-actions">
          <button class="toolbar-btn"
                  (click)="onForward()"
                  [disabled]="!selectedMessageCount"
                  matTooltip="Forward selected message">
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

  @Output() forward = new EventEmitter<void>();
  @Output() deleteSelected = new EventEmitter<void>();
  @Output() deleteAll = new EventEmitter<void>();

  onForward(): void {
    this.forward.emit();
  }

  onDeleteSelected(): void {
    if (this.selectedMessageCount > 0) {
      const message = this.selectedMessageCount === 1
        ? 'Are you sure you want to delete this message?'
        : `Are you sure you want to delete these ${this.selectedMessageCount} messages?`;

      if (confirm(message)) {
        this.deleteSelected.emit();
      }
    }
  }

  onDeleteAll(): void {
    if (this.totalMessageCount > 0) {
      const message = `Are you sure you want to delete all ${this.totalMessageCount} messages? This action cannot be undone.`;

      if (confirm(message)) {
        this.deleteAll.emit();
      }
    }
  }
}
