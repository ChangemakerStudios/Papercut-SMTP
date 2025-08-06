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
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { MessageService } from '../../services/message.service';
import { DetailDto, RefDto } from '../../models';

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
    FileSizePipe, 
    EmailListPipe, 
    CidTransformPipe
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
            
            <!-- Action Buttons -->
            <div class="action-section flex-shrink-0 flex items-center gap-2">
              <button 
                mat-raised-button 
                color="accent" 
                (click)="downloadRaw()" 
                [disabled]="!messageData.detail"
                class="download-btn flex items-center gap-2 shadow-lg hover:shadow-xl transition-all duration-200">
                <mat-icon>download</mat-icon>
                <span class="hidden sm:inline">Download Raw</span>
              </button>
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
              <div *ngIf="messageData.detail?.bCc?.length" class="flex items-start gap-2 p-2 bg-red-50 dark:bg-red-900/20 rounded-lg">
                <div class="flex items-center justify-center w-8 h-8 bg-red-100 dark:bg-red-800/40 rounded-full flex-shrink-0">
                  <mat-icon class="text-red-600 dark:text-red-400 flex items-center justify-center">visibility_off</mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">BCC</h4>
                  <p class="text-gray-700 dark:text-gray-300 text-sm break-words">{{ messageData.detail?.bCc | emailList }}</p>
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
                    <iframe
                      class="w-full h-full"
                      [srcdoc]="getMessageContent(messageData.detail)"
                      sandbox="allow-same-origin"
                      frameborder="0">
                    </iframe>
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
              <mat-tab label="Sections" [disabled]="!getMessageSections(messageData.detail).length">
                <div class="h-full overflow-auto bg-gray-50 dark:bg-gray-900">
                  <div class="p-3 space-y-4">
                    <div *ngFor="let section of getMessageSections(messageData.detail)" class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-3">
                      <div class="flex items-center gap-3">
                        <mat-icon class="text-gray-600 dark:text-gray-400">{{ getSectionIcon(section.type) }}</mat-icon>
                        <div class="flex-1">
                          <div class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ section.type }}</div>
                          <div class="text-gray-600 dark:text-gray-400 text-xs">{{ section.info }}</div>
                        </div>
                        <button 
                          mat-icon-button 
                          color="primary"
                          (click)="downloadSection(messageData.detail, section)"
                          title="Download attachment">
                          <mat-icon>download</mat-icon>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </mat-tab>

              <!-- Raw Tab -->
              <mat-tab label="Raw">
                <div class="h-full overflow-hidden bg-gray-50 dark:bg-gray-900">
                  <div class="h-full p-3 overflow-auto">
                    <div class="h-full p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-auto">
                      <pre class="whitespace-pre-wrap font-mono text-xs text-gray-900 dark:text-gray-100">{{ getRawContent(messageData.detail) }}</pre>
                    </div>
                  </div>
                </div>
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

  downloadRaw() {
    if (this.currentMessage && (this.currentMessage.id || this.currentMessage.name)) {
      const messageId = this.currentMessage?.name ?? this.currentMessage?.id ?? '';
      console.log('Downloading raw message', this.currentMessage);
      this.messageService.downloadRawMessage(messageId);
    }
  }

  downloadSection(message: DetailDto, section: { type: string; info: string; content?: string }) {
    // Find the original section in message.sections to get the ID
    if (message.id && message.sections) {
      const originalSection = message.sections.find(s => 
        (s.fileName || s.mediaType) === section.type && s.mediaType === section.info
      );
      if (originalSection && originalSection.id) {
        this.messageService.downloadSectionByContentId(message.id, originalSection.id);
      }
    }
  }



  getSectionIcon(type: string): string {
    const lowerType = type.toLowerCase();
    
    if (lowerType.includes('image') || lowerType.includes('.jpg') || lowerType.includes('.png') || lowerType.includes('.gif')) {
      return 'image';
    } else if (lowerType.includes('text') || lowerType.includes('.txt')) {
      return 'description';
    } else if (lowerType.includes('pdf')) {
      return 'picture_as_pdf';
    } else if (lowerType.includes('word') || lowerType.includes('document') || lowerType.includes('.doc')) {
      return 'article';
    } else if (lowerType.includes('spreadsheet') || lowerType.includes('excel') || lowerType.includes('.xls')) {
      return 'table_chart';
    } else if (lowerType.includes('zip') || lowerType.includes('archive') || lowerType.includes('.zip')) {
      return 'archive';
    } else {
      return 'attach_file';
    }
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

  getMessageSections(message: DetailDto | null): { type: string; info: string; content?: string }[] {
    if (!message || !message.sections || message.sections.length === 0) {
      return [];
    }
    
    return message.sections.map(section => ({
      type: section.fileName || section.mediaType || 'Unknown',
      info: section.mediaType || 'Unknown type',
      content: undefined // Content not available in attachment DTO
    }));
  }

  getRawContent(message: DetailDto | null): string {
    if (!message) return 'Raw content not available';
    
    let raw = '';
    
    // Add headers
    if (message.headers) {
      message.headers.forEach(header => {
        raw += `${header.name}: ${header.value}\n`;
      });
    }
    
    raw += '\n';
    
    // Add body content
    if (message.htmlBody) {
      raw += message.htmlBody;
    } else if (message.textBody) {
      raw += message.textBody;
    }
    
    return raw || 'Raw content not available';
  }
}