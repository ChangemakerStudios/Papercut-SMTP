import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Observable, map, switchMap, catchError, of, EMPTY, startWith, combineLatest, shareReplay } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { MessageService } from '../../services/message.service';
import { DetailDto, RefDto } from '../../models';
import { MessageSectionsComponent } from '../message-sections/message-sections.component';

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
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatListModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    EmailListPipe,
    MessageSectionsComponent,
    SafeIframeComponent,
    MessageRawComponent
  ],
  template: `
    <div class="flex flex-col h-full bg-gray-50 dark:bg-gray-900 transition-colors duration-300">
      
      <!-- Single async pipe to prevent duplicate subscriptions -->
      <ng-container *ngIf="messageData$ | async as messageData; else loadingTemplate">
        <!-- Message Content -->
        <!-- Header Section -->
        <div class="flex-shrink-0 bg-white dark:bg-gray-800 shadow-md border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
          <div class="header-content flex items-center justify-between p-3 lg:p-4">
            <!-- Subject Section -->
            <div class="subject-section flex-1 min-w-0">
              <h1 class="message-title text-xl lg:text-2xl font-semibold text-gray-800 dark:text-white truncate m-0">
                {{ (messageData.detail?.subject || messageData.ref?.subject) || '(No Subject)' }}
              </h1>
              <p class="message-date text-sm text-gray-600 dark:text-gray-400 mt-1">
                {{ (messageData.detail?.createdAt || messageData.ref?.createdAt) | date:'full' }}
              </p>
            </div>
            

          </div>
        </div>

        <!-- Message Details Section -->
        <div class="flex-shrink-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
          <div class="message-details-content p-3 lg:p-4">
            <div class="details-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
              
              <!-- From Section -->
              <div class="flex items-start gap-2 p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-blue-100 dark:bg-blue-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-blue-600 dark:text-blue-400 flex items-center justify-center">person</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">From</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm break-words">{{ (messageData.detail?.from || messageData.ref?.from) | emailList }}</p>
                </div>
              </div>
              
              <!-- To Section -->
              <div *ngIf="messageData.detail?.to?.length" class="flex items-start gap-2 p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-green-100 dark:bg-green-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-green-600 dark:text-green-400 flex items-center justify-center">people</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">To</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm break-words">{{ messageData.detail?.to | emailList }}</p>
                </div>
              </div>
              
              <!-- CC Section -->
              <div *ngIf="messageData.detail?.cc?.length" class="flex items-start gap-2 p-2 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-yellow-100 dark:bg-yellow-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-yellow-600 dark:text-yellow-400 flex items-center justify-center">people_outline</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">CC</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm break-words">{{ messageData.detail?.cc | emailList }}</p>
                </div>
              </div>
              
              <!-- BCC Section -->
              <div *ngIf="messageData.detail?.bcc?.length" class="flex items-start gap-2 p-2 bg-red-50 dark:bg-red-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-red-100 dark:bg-red-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-red-600 dark:text-red-400 flex items-center justify-center">visibility_off</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">BCC</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm break-words">{{ messageData.detail?.bcc | emailList }}</p>
                </div>
              </div>
              
              <!-- Attachments Summary -->
              <div *ngIf="messageData.detail?.attachments?.length || messageData.ref?.attachmentCount" class="flex items-start gap-2 p-2 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-purple-100 dark:bg-purple-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-purple-600 dark:text-purple-400 flex items-center justify-center">attach_file</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">Attachments</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm">{{ (messageData.detail?.attachments?.length ?? messageData.ref?.attachmentCount ?? 0) }} attachment(s)</p>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Content Section with Tabs -->
        <div class="flex-1 overflow-hidden bg-gray-50 dark:bg-gray-900">
          <div class="h-full">
            <!-- Loading State for Tabs -->
            <div *ngIf="messageData.isLoadingDetail" class="h-full flex items-center justify-center">
              <div class="text-center p-8">
                <mat-spinner diameter="48" strokeWidth="4" class="mx-auto mb-4"></mat-spinner>
                <h3 class="text-lg text-gray-600 dark:text-gray-300 mb-2">Loading message content...</h3>
                <p class="text-gray-500 dark:text-gray-400">Please wait while we fetch the message details.</p>
              </div>
            </div>
            
            <!-- Tabs Content -->
            <mat-tab-group *ngIf="!messageData.isLoadingDetail && messageData.detail" class="h-full" dynamicHeight="false" animationDuration="0ms">
              
              <!-- Message Tab (HTML iframe view) -->
              <mat-tab label="Message">
                <div class="h-full overflow-hidden bg-white dark:bg-gray-800">
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
                <div class="h-full overflow-auto bg-gray-50 dark:bg-gray-900">
                  <div class="p-3 space-y-2">
                    <div *ngFor="let header of getMessageHeaders(messageData.detail)" class="flex flex-col sm:flex-row sm:items-center p-2 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
                      <span class="font-semibold text-gray-800 dark:text-gray-100 text-sm mr-2 min-w-0 font-mono">{{ header.name }}:</span>
                      <span class="text-gray-700 dark:text-gray-300 text-sm font-mono break-all">{{ header.value }}</span>
                    </div>
                  </div>
                </div>
              </mat-tab>

              <!-- Body Tab (Plain text) -->
              <mat-tab label="Body">
                <div class="h-full overflow-hidden bg-gray-50 dark:bg-gray-900">
                  <div class="h-full p-3 overflow-auto">
                    <div class="h-full p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-auto">
                      <pre class="whitespace-pre-wrap font-mono text-sm text-gray-900 dark:text-gray-100">{{ getTextContent(messageData.detail) }}</pre>
                    </div>
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
        <div class="flex-1 flex items-center justify-center min-h-96 bg-gray-50 dark:bg-gray-900">
          <div class="text-center p-8">
            <mat-icon class="text-6xl text-gray-400 dark:text-gray-500 mb-4 animate-pulse !w-auto !h-auto">email</mat-icon>
            <h2 class="text-xl text-gray-600 dark:text-gray-300 mb-2">Loading message...</h2>
            <p class="text-gray-500 dark:text-gray-400">Please wait while we fetch the message details.</p>
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

    /* Dark mode iframe background */
    :host-context([data-theme="dark"]) iframe {
      background: #1f2937;
    }
  `]
})
export class MessageDetailComponent {
  messageData$: Observable<MessageViewData>;
  private currentMessage: DetailDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    this.messageData$ = this.route.params.pipe(
      switchMap(params => {
        const messageId = params['id'];
        if (messageId) {
          console.log('Loading message with ID:', messageId);
          console.log('Message ID length:', messageId.length);
          console.log('URL decoded message ID:', decodeURIComponent(messageId));
          
          // Get RefDto first (fast)
          const refMessage$ = this.messageService.getMessageRef(messageId);
          
          // Get DetailDto (slower)
          const detailMessage$ = this.messageService.getMessage(messageId).pipe(
            map(detail => {
              console.log('Message detail loaded successfully:', detail);
              this.currentMessage = detail;
              return detail;
            }),
            catchError(error => {
              console.error('Error loading message detail:', error);
              
              // Check if it's a 404 or other error
              if (error.status === 404) {
                console.log('Message not found (404), redirecting to home');
                this.redirectToHome('Message not found');
              } else {
                console.log('Unknown error occurred, redirecting to home');
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
        console.error('No message ID found in route parameters');
        this.redirectToHome('No message ID provided');
        return EMPTY;
      }),
      shareReplay(1)
    );
  }

  private redirectToHome(reason: string): void {
    console.log(`Redirecting to home: ${reason}`);
    // Navigate to the parent route (home) and replace the current history entry
    this.router.navigate(['/']).then(() => {
      console.log('Successfully redirected to home');
    }).catch(err => {
      console.error('Failed to redirect to home:', err);
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