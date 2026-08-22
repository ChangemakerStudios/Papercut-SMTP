import { Component, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, takeUntil, catchError, of, switchMap, tap } from 'rxjs';
import { DetailDto, RefDto } from '../../models';
import { MessageService } from '../../services/message.service';

@Component({
  selector: 'app-message-raw',
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
export class MessageRawComponent implements OnChanges, OnDestroy {
  @Input() message: DetailDto | RefDto | null = null;

  rawContent: string = '';
  isLoading: boolean = false;
  error: string = '';

  private destroy$ = new Subject<void>();

  /**
   * Loading on ngOnInit alone was enough back when the tab group was rebuilt
   * per message. It is not any more -- the tabs stay mounted and this component
   * lives across messages, so it has to reload whenever the input changes or it
   * keeps showing the first message it ever saw.
   */
  private readonly load$ = new Subject<string>();

  constructor(private messageService: MessageService) {
    this.load$
      .pipe(
        tap(() => {
          this.isLoading = true;
          this.error = '';
          this.rawContent = '';
        }),
        // switchMap, so a slow response for the message you just left cannot
        // land on top of the one you are looking at now
        switchMap(messageId =>
          this.messageService.getRawContent(messageId).pipe(
            catchError(error => {
              this.error = error.message || 'Unknown error occurred';
              return of('');
            })
          )
        ),
        takeUntil(this.destroy$)
      )
      .subscribe((content: string) => {
        this.rawContent = content;
        this.isLoading = false;
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['message']) return;

    // null while the next message's detail is in flight -- that is a load, not
    // an error, and showing the previous message's raw text would be a lie
    if (!this.message) {
      this.rawContent = '';
      this.error = '';
      this.isLoading = true;
      return;
    }

    const messageId = this.message.name ?? this.message.id ?? '';
    if (!messageId) {
      this.error = 'Invalid message ID';
      return;
    }

    this.load$.next(messageId);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
