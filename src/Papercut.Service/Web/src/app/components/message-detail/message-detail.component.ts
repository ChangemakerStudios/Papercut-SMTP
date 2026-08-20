import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Observable, map, switchMap, catchError, of, EMPTY, startWith, combineLatest, shareReplay } from 'rxjs';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LucideAngularModule, Mail, Paperclip } from 'lucide-angular';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { DownloadButtonDirective } from '../../directives/download-button.directive';
import { EmailSectionDto } from '../../models';
import { MessageService } from '../../services/message.service';
import { MessageApiService } from '../../services/message-api.service';
import { LoggingService } from '../../services/logging.service';
import { DetailDto, RefDto } from '../../models';
import { MessageSectionsComponent } from '../message-sections/message-sections.component';
import { MessageHeaderComponent } from './message-header.component';

import { SafeIframeComponent } from '../safe-iframe/safe-iframe.component';
import { MessageRawComponent } from '../message-raw/message-raw.component';

interface MessageViewData {
  ref: RefDto | null;
  detail: DetailDto | null;
  /** Service-rendered, sanitized body html (null until it arrives) */
  html: string | null;
  /** True once the render request failed and the raw body is all we have */
  htmlFailed: boolean;
  isLoadingDetail: boolean;
}

@Component({
  selector: 'app-message-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    LucideAngularModule,
    FileSizePipe,
    DownloadButtonDirective,
    MessageSectionsComponent,
    MessageHeaderComponent,
    SafeIframeComponent,
    MessageRawComponent
  ],
  template: `
    <div class="flex flex-col h-full bg-surface transition-colors duration-300">

      <!-- Single async pipe to prevent duplicate subscriptions -->
      <ng-container *ngIf="messageData$ | async as messageData; else loadingTemplate">

        <!-- Labeled header fields (desktop-style From/To/Date/Subject rows) -->
        <app-message-header [message]="messageData"></app-message-header>

        <!-- Content Section with Tabs.
             The tab group and its iframe are deliberately NOT behind an *ngIf:
             tearing them down per message meant rebuilding Material's tab
             group and a fresh iframe on every click, which is most of the
             click-to-render cost. They stay mounted and the content swaps. -->
        <div class="flex-1 overflow-hidden bg-surface message-tabs relative">
          <div class="h-full">
            <!-- Loading veil over the (still mounted) tabs -->
            <div *ngIf="messageData.isLoadingDetail" class="loading-veil">
              <mat-spinner diameter="36" strokeWidth="3"></mat-spinner>
            </div>

            <!-- Tabs Content -->
            <mat-tab-group class="h-full" dynamicHeight="false" animationDuration="0ms"
                           [selectedIndex]="selectedTabIndex"
                           (selectedIndexChange)="selectedTabIndex = $event">

              <!-- Message Tab (HTML iframe view) -->
              <mat-tab label="Message">
                <div class="h-full overflow-hidden bg-surface flex flex-col">
                  <div class="flex-1 min-h-0">
                    <app-safe-iframe
                      class="h-full"
                      [content]="getMessageContent(messageData)">
                    </app-safe-iframe>
                  </div>

                  <!-- Attachments bar (desktop-style, bottom of the message view) -->
                  <div class="attachments-bar" *ngIf="messageData.detail?.attachments?.length">
                    <span class="attachments-label">Attachments</span>
                    <div class="attachments-chips">
                      <button class="attachment-chip"
                              *ngFor="let att of messageData.detail?.attachments"
                              [appDownloadButton]="'attach-' + (att.id || att.index)"
                              [downloadUrl]="buildAttachmentUrl(messageData.detail, att)"
                              [downloadFilename]="att.fileName || 'attachment-' + (att.index ?? 0)"
                              [title]="'Download ' + (att.fileName || att.mediaType || 'attachment')">
                        <lucide-icon [img]="icons.Paperclip" [size]="14"></lucide-icon>
                        <span class="chip-name">{{ att.fileName || att.mediaType || 'attachment' }}</span>
                        <span class="chip-size" *ngIf="att.size != null">{{ att.size | fileSize }}</span>
                      </button>
                    </div>
                  </div>
                </div>
              </mat-tab>

              <!-- Headers Tab -->
              <mat-tab label="Headers">
                <div class="h-full overflow-auto bg-surface">
                  <div class="p-4 headers-content">
                    <div *ngFor="let header of getMessageHeaders(messageData.detail)" class="header-item">
                      <span class="header-name">{{ header.name }}:</span><span class="header-value">{{ header.value }}</span>
                    </div>
                  </div>
                </div>
              </mat-tab>

              <!-- Body Tab (Plain text) -->
              <mat-tab label="Body">
                <div class="h-full overflow-hidden bg-surface">
                  <div class="h-full p-4 overflow-auto">
                    <pre class="whitespace-pre-wrap font-mono text-[12px] leading-relaxed text-ink">{{ getTextContent(messageData.detail) }}</pre>
                  </div>
                </div>
              </mat-tab>

              <!-- Sections Tab -->
              <mat-tab label="Sections" [disabled]="!messageData.detail?.sections?.length">
                <app-message-sections [message]="messageData.detail"></app-message-sections>
              </mat-tab>

              <!-- Raw Tab -->
              <mat-tab label="Raw">
                <app-message-raw [message]="messageData.detail"></app-message-raw>
              </mat-tab>

            </mat-tab-group>
          </div>
        </div>
      </ng-container>

      <!-- Loading Template -->
      <ng-template #loadingTemplate>
        <div class="flex-1 flex items-center justify-center min-h-96 bg-surface">
          <div class="text-center p-8">
            <lucide-icon [img]="icons.Mail" [size]="56" class="loading-mail-icon"></lucide-icon>
            <h2 class="text-xl text-muted mb-2">Loading message...</h2>
            <p class="text-faint">Please wait while we fetch the message details.</p>
          </div>
        </div>
      </ng-template>
    </div>

  `,
  styles: [`
    /* Essential iframe styles for message content */
    iframe {
      border: none;
      background: white;
    }

    .loading-veil {
      position: absolute;
      inset: 0;
      z-index: 5;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--pc-surface);
    }

    .loading-mail-icon {
      display: inline-block;
      color: var(--pc-faint);
      margin-bottom: 1rem;
      animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    }

    /* Desktop-style attachments row: one line along the bottom */
    .attachments-bar {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 16px;
      border-top: 1px solid var(--pc-border);
      background: var(--pc-surface-2);
      min-width: 0;
    }

    .attachments-label {
      flex-shrink: 0;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--pc-faint);
      user-select: none;
    }

    .attachments-chips {
      display: flex;
      align-items: center;
      gap: 8px;
      overflow-x: auto;
      min-width: 0;
      padding: 2px 0;
    }

    .attachment-chip {
      flex-shrink: 0;
      display: inline-flex;
      align-items: center;
      gap: 7px;
      padding: 5px 14px;
      border: 1px solid var(--pc-border);
      border-radius: 999px;
      background: var(--pc-surface);
      color: var(--pc-accent-text);
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.12s ease, border-color 0.12s ease;
    }

    .attachment-chip:hover {
      background: var(--pc-hover);
      border-color: var(--pc-accent);
    }

    .chip-size {
      font-family: var(--pc-font-mono);
      font-size: 11.5px;
      font-weight: 400;
      color: var(--pc-muted);
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }
  `]
})
export class MessageDetailComponent {
  protected readonly icons = { Mail, Paperclip };

  /** Survives switching between messages so the active tab is kept */
  selectedTabIndex = 0;

  messageData$: Observable<MessageViewData>;
  private currentMessage: DetailDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService,
    private messageApiService: MessageApiService,
    private loggingService: LoggingService
  ) {
    this.messageData$ = this.route.params.pipe(
      switchMap(params => {
        const messageId = params['id'];
        if (messageId) {
          this.loggingService.debug('Loading message with ID', { 
            messageId, 
            length: messageId.length, 
            decoded: decodeURIComponent(messageId) 
          });
          
          // Get RefDto first (fast)
          const refMessage$ = this.messageApiService.getMessageRef(messageId);
          
          // Get DetailDto (slower)
          const detailMessage$ = this.messageApiService.getMessageDetail(messageId).pipe(
            map(detail => {
              this.loggingService.debug('Message detail loaded successfully', { messageId: detail.id });
              this.currentMessage = detail;
              return detail;
            }),
            catchError(error => {
              this.loggingService.error('Error loading message detail', error);
              
              // Check if it's a 404 or other error
              if (error.status === 404) {
                this.loggingService.info('Message not found (404), redirecting to home');
                this.redirectToHome('Message not found');
              } else {
                this.loggingService.warn('Unknown error occurred, redirecting to home');
                this.redirectToHome('Failed to load message');
              }
              
              return of(null);
            })
          );
          
          // Body html comes pre-rendered and sanitized from the service
          const html$ = this.messageApiService.getMessageHtml(messageId).pipe(
            map(html => ({ html: html as string | null, failed: false })),
            catchError(error => {
              this.loggingService.warn('Falling back to the raw body: rendered html failed', error);
              return of({ html: null, failed: true });
            }),
            startWith({ html: null, failed: false })
          );

          // Combine RefDto, DetailDto and the rendered html
          return combineLatest([refMessage$, detailMessage$.pipe(startWith(null)), html$]).pipe(
            map(([ref, detail, rendered]) => ({
              ref,
              detail,
              html: rendered.html,
              htmlFailed: rendered.failed,
              // hold the view until the body is ready too: detail and html are
              // requested together and land within a few ms of each other, so
              // waiting gives one clean transition instead of spinner -> tabs
              // -> "Loading..." placeholder -> content
              isLoadingDetail: detail === null || (rendered.html === null && !rendered.failed)
            }))
          );
        }
        this.loggingService.error('No message ID found in route parameters');
        this.redirectToHome('No message ID provided');
        return EMPTY;
      }),
      shareReplay(1)
    );
  }

  private redirectToHome(reason: string): void {
    this.loggingService.info(`Redirecting to home: ${reason}`);
    // Navigate to the parent route (home) and replace the current history entry
    this.router.navigate(['/']).then(() => {
      this.loggingService.debug('Successfully redirected to home');
    }).catch(err => {
      this.loggingService.error('Failed to redirect to home', err);
    });
  }

  buildAttachmentUrl(message: DetailDto | null, att: EmailSectionDto): string {
    if (!message?.id) return '';

    const encodedMessageId = encodeURIComponent(message.id);

    if (att.id) {
      return `/api/messages/${encodedMessageId}/contents/${encodeURIComponent(att.id)}`;
    }

    return `/api/messages/${encodedMessageId}/sections/${att.index ?? 0}`;
  }

  getMessageContent(messageData: MessageViewData): string {
    if (messageData.html) {
      // already sanitized and cid-resolved by the service -- just theme it
      return this.messageService.styleRenderedHtml(messageData.html);
    }

    // Only reach for the raw body when the render actually failed: showing it
    // while the request is still in flight would put unsanitized markup
    // (scripts, unresolved cid: refs) into the frame for those few frames
    if (!messageData.htmlFailed) return '';

    if (!messageData.detail) return '<html><body>No message content available.</body></html>';

    return this.messageService.getMessageContent(messageData.detail);
  }

  getMessageHeaders(message: DetailDto | null) {
    if (!message) return [];
    return message.headers || [];
  }

  getTextContent(message: DetailDto | null): string {
    if (!message) return 'No message body available.';
    if (message.textBody) {
      return message.textBody;
    } else if (message.htmlBody) {
      // Strip HTML tags for plain text view
      return message.htmlBody.replace(/<[^>]*>/g, '');
    } else {
      return 'No message body available.';
    }
  }
}