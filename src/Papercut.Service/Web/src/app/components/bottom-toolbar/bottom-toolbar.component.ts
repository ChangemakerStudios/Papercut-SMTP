import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-bottom-toolbar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <div class="bottom-toolbar">
      <div class="toolbar-container">
        <!-- Left side - Action buttons -->
        <div class="toolbar-actions">
          <button mat-stroked-button 
                  class="toolbar-btn"
                  (click)="onForward()"
                  [disabled]="!selectedMessageCount"
                  matTooltip="Forward selected message">
            <mat-icon>forward</mat-icon>
            <span>FORWARD</span>
          </button>
          
          <button mat-stroked-button 
                  class="toolbar-btn delete-btn"
                  (click)="onDeleteSelected()"
                  [disabled]="!selectedMessageCount"
                  matTooltip="Delete selected message(s)">
            <mat-icon>delete</mat-icon>
            <span>DELETE ({{ selectedMessageCount }})</span>
          </button>
          
          <button mat-stroked-button 
                  class="toolbar-btn delete-all-btn"
                  (click)="onDeleteAll()"
                  [disabled]="!totalMessageCount"
                  matTooltip="Delete all messages">
            <mat-icon>delete_sweep</mat-icon>
            <span>DELETE ALL</span>
          </button>
        </div>
        
        <!-- Right side - Status info -->
        <div class="toolbar-status">
          <span class="status-text">
            Papercut SMTP v 7.0.0.0 - 
            <a href="https://github.com/ChangemakerStudios/Papercut-SMTP" 
               target="_blank" 
               class="status-link">
              https://github.com/ChangemakerStudios/Papercut-SMTP
            </a>
          </span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bottom-toolbar {
      background-color: #f8f9fa;
      border-top: 1px solid #dee2e6;
      padding: 8px 16px;
      min-height: 48px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    }

    :host-context([data-theme="dark"]) .bottom-toolbar {
      background-color: #2d2d30;
      border-top-color: #3e3e42;
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
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      font-size: 11px;
      font-weight: 500;
      text-transform: uppercase;
      min-width: auto;
      height: 32px;
      border-radius: 2px;
      border: 1px solid #ccc;
      background-color: #ffffff;
      color: #333;
      transition: all 0.2s ease;
    }

    :host-context([data-theme="dark"]) .toolbar-btn {
      background-color: #3e3e42;
      border-color: #555;
      color: #ffffff;
    }

    .toolbar-btn:hover:not(:disabled) {
      background-color: #f0f0f0;
      border-color: #999;
    }

    :host-context([data-theme="dark"]) .toolbar-btn:hover:not(:disabled) {
      background-color: #4a4a4e;
      border-color: #666;
    }

    .toolbar-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .toolbar-btn mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .delete-btn:hover:not(:disabled) {
      background-color: #ffebee;
      border-color: #f44336;
      color: #f44336;
    }

    .delete-all-btn:hover:not(:disabled) {
      background-color: #ffebee;
      border-color: #d32f2f;
      color: #d32f2f;
    }

    :host-context([data-theme="dark"]) .delete-btn:hover:not(:disabled),
    :host-context([data-theme="dark"]) .delete-all-btn:hover:not(:disabled) {
      background-color: #4a2f2f;
      border-color: #ff5252;
      color: #ff5252;
    }

    .toolbar-status {
      display: flex;
      align-items: center;
    }

    .status-text {
      font-size: 10px;
      color: #666;
    }

    :host-context([data-theme="dark"]) .status-text {
      color: #aaa;
    }

    .status-link {
      color: #0066cc;
      text-decoration: none;
    }

    .status-link:hover {
      text-decoration: underline;
    }

    :host-context([data-theme="dark"]) .status-link {
      color: #4a9eff;
    }

    /* Mobile responsiveness */
    @media (max-width: 768px) {
      .toolbar-container {
        flex-direction: column;
        gap: 8px;
        align-items: stretch;
      }
      
      .toolbar-actions {
        justify-content: center;
      }
      
      .toolbar-status {
        justify-content: center;
      }
      
      .status-text {
        text-align: center;
      }
    }

    @media (max-width: 480px) {
      .toolbar-btn span {
        display: none;
      }
      
      .toolbar-btn {
        padding: 8px;
        min-width: 40px;
      }
    }
  `]
})
export class BottomToolbarComponent {
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