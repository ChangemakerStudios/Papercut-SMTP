import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, takeUntil, catchError, of } from 'rxjs';
import { DetailDto, RefDto } from '../../models';
import { MessageService } from '../../services/message.service';

@Component({
  selector: 'app-message-raw',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="h-full overflow-hidden bg-gray-50 dark:bg-gray-900">
      <div class="h-full p-3 overflow-auto">
        <div class="h-full p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-auto">
          <!-- Loading State -->
          <div *ngIf="isLoading" class="flex items-center justify-center py-8">
            <mat-spinner diameter="32"></mat-spinner>
            <span class="ml-3 text-sm text-gray-600 dark:text-gray-400">Loading raw content...</span>
          </div>
          
          <!-- Error State -->
          <div *ngIf="error && !isLoading" class="text-center py-8">
            <p class="text-red-600 dark:text-red-400 text-sm mb-2">Failed to load raw content</p>
            <p class="text-gray-500 dark:text-gray-400 text-xs">{{ error }}</p>
          </div>
          
          <!-- Raw Content -->
          <code *ngIf="rawContent && !isLoading" 
                class="whitespace-pre-wrap text-xs text-gray-900 dark:text-gray-100 font-mono leading-tight block">{{ rawContent }}</code>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }
    
    code {
      font-family: 'Courier New', Consolas, Monaco, 'Andale Mono', 'Ubuntu Mono', monospace;
      font-size: 11px;
      line-height: 1.3;
      white-space: pre-wrap;
      word-wrap: break-word;
      overflow-wrap: break-word;
    }
  `]
})
export class MessageRawComponent implements OnInit, OnDestroy {
  @Input() message: DetailDto | RefDto | null = null;
  
  rawContent: string = '';
  isLoading: boolean = false;
  error: string = '';
  
  private destroy$ = new Subject<void>();

  constructor(private messageService: MessageService) {}

  ngOnInit(): void {
    if (this.message) {
      this.loadRawContent();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadRawContent(): void {
    if (!this.message) {
      this.error = 'No message provided';
      return;
    }

    const messageId = this.message.name ?? this.message.id ?? '';
    if (!messageId) {
      this.error = 'Invalid message ID';
      return;
    }

    this.isLoading = true;
    this.error = '';
    this.rawContent = '';

    this.messageService.getRawContent(messageId)
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          // Error loading raw content - handled in error property
          this.error = error.message || 'Unknown error occurred';
          return of('');
        })
      )
      .subscribe((content: string) => {
        this.rawContent = content;
        this.isLoading = false;
      });
  }
}
