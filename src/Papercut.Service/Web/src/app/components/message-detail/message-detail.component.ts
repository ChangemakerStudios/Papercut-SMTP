import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Observable, map, switchMap, catchError, of, EMPTY, startWith, combineLatest, shareReplay } from 'rxjs';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LucideAngularModule, Mail } from 'lucide-angular';
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

        <!-- Content Section with Tabs -->
        <div class="flex-1 overflow-hidden bg-surface message-tabs">
          <div class="h-full">
            <!-- Loading State for Tabs -->
            <div *ngIf="messageData.isLoadingDetail" class="h-full flex items-center justify-center">
              <div class="text-center p-8">
                <mat-spinner diameter="48" strokeWidth="4" class="mx-auto mb-4"></mat-spinner>
                <h3 class="text-lg text-muted mb-2">Loading message content...</h3>
                <p class="text-faint">Please wait while we fetch the message details.</p>
              </div>
            </div>

            <!-- Tabs Content -->
            <mat-tab-group *ngIf="!messageData.isLoadingDetail && messageData.detail" class="h-full" dynamicHeight="false" animationDuration="0ms">

              <!-- Message Tab (HTML iframe view) -->
              <mat-tab label="Message">
                <div class="h-full overflow-hidden bg-surface">
                  <div class="h-full">
                    <app-safe-iframe
                      class="h-full"
                      [content]="getMessageContent(messageData.detail)">
                    </app-safe-iframe>
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
              <mat-tab label="Sections" [disabled]="!messageData.detail.sections?.length">
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

    .loading-mail-icon {
      display: inline-block;
      color: var(--pc-faint);
      margin-bottom: 1rem;
      animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }
  `]
})
export class MessageDetailComponent {
  protected readonly icons = { Mail };

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
          
          // Combine RefDto and DetailDto
          return combineLatest([refMessage$, detailMessage$.pipe(startWith(null))]).pipe(
            map(([ref, detail]) => ({
              ref,
              detail,
              isLoadingDetail: detail === null
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

  getMessageContent(message: DetailDto | null): string {
    if (!message) return '<html><body>No message content available.</body></html>';
    return this.messageService.getMessageContent(message);
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