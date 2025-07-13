import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Observable, map, switchMap, catchError, of, EMPTY } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { MessageService } from '../../services/message.service';
import { DetailDto } from '../../models';

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
    FileSizePipe, 
    EmailListPipe, 
    CidTransformPipe
  ],
  template: `
    <div class="message-detail-container flex flex-col h-full bg-gray-50 transition-colors duration-300">
      
      <!-- Single async pipe to prevent duplicate subscriptions -->
      <ng-container *ngIf="message$ | async as message; else loadingTemplate">
        <!-- Message Content -->
        <!-- Header Section -->
        <div class="header-section flex-shrink-0 bg-white shadow-md border-b border-gray-200 transition-colors duration-300">
          <div class="header-content flex items-center justify-between p-3 lg:p-4">
            <!-- Subject Section -->
            <div class="subject-section flex-1 min-w-0">
              <h1 class="message-title text-xl lg:text-2xl font-semibold text-gray-800 dark:text-white truncate m-0">
                {{ message.subject || '(No Subject)' }}
              </h1>
              <p class="message-date text-sm text-gray-600 dark:text-gray-400 mt-1">
                {{ message.createdAt | date:'full' }}
              </p>
            </div>
            
            <!-- Action Buttons -->
            <div class="action-section flex-shrink-0 flex items-center gap-2">
              <button 
                mat-raised-button 
                color="accent" 
                (click)="downloadRaw()" 
                class="download-btn flex items-center gap-2 shadow-lg hover:shadow-xl transition-all duration-200">
                <mat-icon>download</mat-icon>
                <span class="hidden sm:inline">Download Raw</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Message Details Section -->
        <div class="message-details-section flex-shrink-0 bg-white border-b border-gray-200 transition-colors duration-300">
          <div class="message-details-content p-3 lg:p-4">
            <div class="details-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
              
              <!-- From Section -->
              <div class="detail-item flex items-start gap-2 p-2 bg-blue-50 rounded-lg">
                <div class="detail-icon flex items-center justify-center w-8 h-8 bg-blue-100 rounded-full flex-shrink-0">
                  <mat-icon class="text-blue-600 flex items-center justify-center">person</mat-icon>
                </div>
                <div class="detail-content flex-1 min-w-0">
                  <h4 class="detail-label font-semibold text-gray-800 text-sm mb-0.5">From</h4>
                  <p class="detail-value text-gray-700 text-sm break-words">{{ message.from | emailList }}</p>
                </div>
              </div>
              
              <!-- To Section -->
              <div class="detail-item flex items-start gap-2 p-2 bg-green-50 rounded-lg">
                <div class="detail-icon flex items-center justify-center w-8 h-8 bg-green-100 rounded-full flex-shrink-0">
                  <mat-icon class="text-green-600 flex items-center justify-center">people</mat-icon>
                </div>
                <div class="detail-content flex-1 min-w-0">
                  <h4 class="detail-label font-semibold text-gray-800 text-sm mb-0.5">To</h4>
                  <p class="detail-value text-gray-700 text-sm break-words">{{ message.to | emailList }}</p>
                </div>
              </div>
              
              <!-- CC Section -->
              <div *ngIf="message.cc?.length" class="detail-item flex items-start gap-2 p-2 bg-yellow-50 rounded-lg">
                <div class="detail-icon flex items-center justify-center w-8 h-8 bg-yellow-100 rounded-full flex-shrink-0">
                  <mat-icon class="text-yellow-600 flex items-center justify-center">people_outline</mat-icon>
                </div>
                <div class="detail-content flex-1 min-w-0">
                  <h4 class="detail-label font-semibold text-gray-800 text-sm mb-0.5">CC</h4>
                  <p class="detail-value text-gray-700 text-sm break-words">{{ message.cc | emailList }}</p>
                </div>
              </div>
              
              <!-- BCC Section -->
              <div *ngIf="message.bCc?.length" class="detail-item flex items-start gap-2 p-2 bg-red-50 rounded-lg">
                <div class="detail-icon flex items-center justify-center w-8 h-8 bg-red-100 rounded-full flex-shrink-0">
                  <mat-icon class="text-red-600 flex items-center justify-center">visibility_off</mat-icon>
                </div>
                <div class="detail-content flex-1 min-w-0">
                  <h4 class="detail-label font-semibold text-gray-800 text-sm mb-0.5">BCC</h4>
                  <p class="detail-value text-gray-700 text-sm break-words">{{ message.bCc | emailList }}</p>
                </div>
              </div>
              
              <!-- Attachments Summary -->
              <div *ngIf="message.sections?.length" class="detail-item flex items-start gap-2 p-2 bg-purple-50 rounded-lg">
                <div class="detail-icon flex items-center justify-center w-8 h-8 bg-purple-100 rounded-full flex-shrink-0">
                  <mat-icon class="text-purple-600 flex items-center justify-center">attach_file</mat-icon>
                </div>
                <div class="detail-content flex-1 min-w-0">
                  <h4 class="detail-label font-semibold text-gray-800 text-sm mb-0.5">Attachments</h4>
                  <p class="detail-value text-gray-700 text-sm">{{ message.attachments?.length ?? 0 }} attachment(s)</p>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Content Section with Tabs -->
        <div class="content-section flex-1 overflow-hidden">
          <div class="message-tabs h-full">
            <mat-tab-group class="h-full" dynamicHeight="false">
              
              <!-- Message Tab (HTML iframe view) -->
              <mat-tab label="Message">
                <div class="tab-content h-full overflow-hidden">
                  <div class="message-content h-full">
                    <iframe
                      class="message-iframe w-full h-full"
                      [srcdoc]="getMessageContent(message)"
                      sandbox="allow-same-origin"
                      frameborder="0">
                    </iframe>
                  </div>
                </div>
              </mat-tab>

              <!-- Headers Tab -->
              <mat-tab label="Headers">
                <div class="tab-content h-full overflow-auto">
                  <div class="headers-content p-3 space-y-2">
                    <div *ngFor="let header of getMessageHeaders(message)" class="header-item flex flex-col sm:flex-row sm:items-center p-2 bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600">
                      <span class="header-name font-semibold text-gray-800 dark:text-white text-sm mr-2 min-w-0">{{ header.name }}:</span>
                      <span class="header-value text-gray-700 dark:text-gray-300 text-sm">{{ header.value }}</span>
                    </div>
                  </div>
                </div>
              </mat-tab>

              <!-- Body Tab (Plain text) -->
              <mat-tab label="Body">
                <div class="tab-content h-full overflow-hidden">
                  <div class="body-content h-full p-3 overflow-auto">
                    <div class="message-body h-full p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-auto">
                      <pre class="whitespace-pre-wrap font-mono text-sm">{{ getTextContent(message) }}</pre>
                    </div>
                  </div>
                </div>
              </mat-tab>
              
              <!-- Sections Tab -->
              <mat-tab label="Sections" [disabled]="!getMessageSections(message).length">
                <div class="tab-content h-full overflow-auto">
                  <div class="sections-content p-3 space-y-4">
                    <div *ngFor="let section of getMessageSections(message)" class="section-item bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600 p-3">
                                              <div class="section-header flex items-center gap-3">
                          <mat-icon>{{ getSectionIcon(section.type) }}</mat-icon>
                          <div class="flex-1">
                            <div class="section-type font-semibold text-gray-800 dark:text-white text-sm">{{ section.type }}</div>
                            <div class="section-info text-gray-600 dark:text-gray-400 text-xs">{{ section.info }}</div>
                          </div>
                          <button 
                            mat-icon-button 
                            color="primary"
                            (click)="downloadSection(message, section)"
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
                <div class="tab-content h-full overflow-hidden">
                  <div class="raw-content h-full p-3 overflow-auto">
                    <div class="h-full p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-auto">
                      <pre class="whitespace-pre-wrap font-mono text-xs">{{ getRawContent(message) }}</pre>
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
        <div class="loading-container flex-1 flex items-center justify-center min-h-96">
          <div class="loading-content text-center p-8">
            <mat-icon class="loading-icon text-6xl text-gray-400 mb-4 animate-pulse">email</mat-icon>
            <h2 class="text-xl text-gray-600 mb-2">Loading message...</h2>
            <p class="text-gray-500">Please wait while we fetch the message details.</p>
          </div>
        </div>
      </ng-template>
    </div>
  `,
  styles: [`
    /* Only custom CSS that can't be expressed with Tailwind utilities */
    .header-value {
      word-break: break-all;
      font-family: monospace;
    }



    /* Message body content styling */
    .message-body img {
      max-width: 100%;
      height: auto;
      border-radius: 0.5rem;
      box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
    }

    .message-body p {
      margin-bottom: 0.75rem;
    }

    .message-body p:last-child {
      margin-bottom: 0;
    }

    .message-body a {
      color: #2563eb;
      text-decoration: underline;
    }

    .message-body a:hover {
      color: #1d4ed8;
    }

    // Dark theme specific overrides
    :host-context([data-theme="dark"]) {
      .message-detail-container {
        background-color: #111827;
      }

      .message-body {
        color: #ffffff;
        background-color: #1f2937;
      }

      .message-body h1, 
      .message-body h2, 
      .message-body h3, 
      .message-body h4, 
      .message-body h5, 
      .message-body h6 {
        color: #ffffff;
      }

      .message-body p {
        color: #ffffff;
      }

      // Header section dark mode
      .header-section {
        background-color: #1f2937 !important;
        border-color: #374151 !important;
      }

      // Message details section dark mode
      .message-details-section {
        background-color: #1f2937 !important;
        border-color: #374151 !important;
      }

      // Detail items - From (blue)
      .detail-item:nth-child(1) {
        background-color: rgba(59, 130, 246, 0.1) !important;
      }

      .detail-item:nth-child(1) .detail-icon {
        background-color: rgba(59, 130, 246, 0.2) !important;
      }

      .detail-item:nth-child(1) .detail-icon mat-icon {
        color: #93c5fd !important;
      }

      // Detail items - To (green)
      .detail-item:nth-child(2) {
        background-color: rgba(34, 197, 94, 0.1) !important;
      }

      .detail-item:nth-child(2) .detail-icon {
        background-color: rgba(34, 197, 94, 0.2) !important;
      }

      .detail-item:nth-child(2) .detail-icon mat-icon {
        color: #86efac !important;
      }

      // Detail items - CC (yellow)
      .detail-item:nth-child(3) {
        background-color: rgba(234, 179, 8, 0.1) !important;
      }

      .detail-item:nth-child(3) .detail-icon {
        background-color: rgba(234, 179, 8, 0.2) !important;
      }

      .detail-item:nth-child(3) .detail-icon mat-icon {
        color: #fde047 !important;
      }

      // Detail items - BCC (red)
      .detail-item:nth-child(4) {
        background-color: rgba(239, 68, 68, 0.1) !important;
      }

      .detail-item:nth-child(4) .detail-icon {
        background-color: rgba(239, 68, 68, 0.2) !important;
      }

      .detail-item:nth-child(4) .detail-icon mat-icon {
        color: #fca5a5 !important;
      }

      // Detail items - Attachments (purple)
      .detail-item:nth-child(5) {
        background-color: rgba(168, 85, 247, 0.1) !important;
      }

      .detail-item:nth-child(5) .detail-icon {
        background-color: rgba(168, 85, 247, 0.2) !important;
      }

      .detail-item:nth-child(5) .detail-icon mat-icon {
        color: #c4b5fd !important;
      }

      // Common dark mode styles for all detail items
      .detail-label {
        color: #ffffff !important;
      }

      .detail-value {
        color: #d1d5db !important;
      }

      // Tab content dark mode
      .tab-content {
        background-color: #1f2937;
      }

      .body-content .message-body {
        background-color: #1f2937;
        border-color: #374151;
      }

      .headers-content .header-item {
        background-color: #374151;
        border-color: #4b5563;
      }

      .header-name {
        color: #ffffff;
      }

      .header-value {
        color: #d1d5db;
      }

      .sections-content .section-item {
        background-color: #374151;
        border-color: #4b5563;
      }

      .section-type {
        color: #ffffff;
      }

      .section-info {
        color: #9ca3af;
      }

      // Loading state dark mode
      .loading-content h2 {
        color: #ffffff;
      }

      .loading-content p {
        color: #9ca3af;
      }

      .message-body div {
        color: #ffffff;
      }
    }

    // Responsive design
    @media (max-width: 1024px) {
      .content-grid {
        flex-direction: column;
      }
      
      .sidebar-column {
        width: 100%;
        border-left: none;
        border-top: 1px solid;
        @apply border-gray-200 dark:border-gray-700;
      }
    }
  `]
})
export class MessageDetailComponent {
  message$: Observable<DetailDto | null>;
  private currentMessage: DetailDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    this.message$ = this.route.params.pipe(
      switchMap(params => {
        const messageId = params['id'];
        if (messageId) {
          console.log('Loading message with ID:', messageId);
          console.log('Message ID length:', messageId.length);
          console.log('URL decoded message ID:', decodeURIComponent(messageId));
          return this.messageService.getMessage(messageId);
        }
        console.error('No message ID found in route parameters');
        this.redirectToHome('No message ID provided');
        return EMPTY;
      }),
      map(messageDetail => {
        console.log('Message loaded successfully:', messageDetail);
        this.currentMessage = messageDetail;
        return messageDetail;
      }),
      catchError(error => {
        console.error('Error loading message:', error);
        
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

  getMessageContent(message: DetailDto): string {
    return this.messageService.getMessageContent(message);
  }

  getMessageHeaders(message: DetailDto) {
    return message.headers || [];
  }

  getTextContent(message: DetailDto): string {
    if (message.textBody) {
      return message.textBody;
    } else if (message.htmlBody) {
      // Strip HTML tags for plain text view
      return message.htmlBody.replace(/<[^>]*>/g, '');
    } else {
      return 'No message body available.';
    }
  }

  getMessageSections(message: DetailDto): { type: string; info: string; content?: string }[] {
    if (!message.sections || message.sections.length === 0) {
      return [];
    }
    
    return message.sections.map(section => ({
      type: section.fileName || section.mediaType || 'Unknown',
      info: section.mediaType || 'Unknown type',
      content: undefined // Content not available in attachment DTO
    }));
  }

  getRawContent(message: DetailDto): string {
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