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
    <div class="h-full overflow-hidden bg-surface">
      <div class="h-full p-4 overflow-auto">
        <!-- Loading State -->
        <div *ngIf="isLoading" class="flex items-center justify-center py-8">
          <mat-spinner diameter="32"></mat-spinner>
          <span class="ml-3 text-sm text-muted">Loading raw content...</span>
        </div>

        <!-- Error State -->
        <div *ngIf="error && !isLoading" class="text-center py-8">
          <p class="text-danger text-sm mb-2">Failed to load raw content</p>
          <p class="text-muted text-xs">{{ error }}</p>
        </div>

        <!-- Raw Content -->
        <code *ngIf="rawContent && !isLoading" class="raw-code block">{{ rawContent }}</code>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }

    /* Same mono family as the Body tab, one step smaller */
    .raw-code {
      font-family: var(--pc-font-mono);
      font-size: var(--pc-text-read);
      line-height: var(--pc-leading-read);
      color: var(--pc-ink);
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
