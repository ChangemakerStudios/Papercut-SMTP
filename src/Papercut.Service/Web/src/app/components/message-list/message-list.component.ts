import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Observable, map, combineLatest, BehaviorSubject, switchMap, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { Message, MessageResponse, MessageRepository, MessageDetail } from '../../services/message.repository';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';

interface PaginationInfo {
  currentPage: number;
  totalPages: number;
  limit: number;
  start: number;
  totalCount: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatCardModule, 
    MatButtonModule, 
    MatIconModule, 
    MatChipsModule,
    FileSizePipe,
    EmailListPipe,
    CidTransformPipe
  ],
  template: `
    <div class="email-client-container flex flex-col h-full bg-gray-50 dark:bg-gray-900 transition-colors duration-300">
      <!-- Header Section -->
      <div class="header-section flex-shrink-0 p-4 lg:p-6 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div class="header-content flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div class="title-section flex items-center gap-3">
            <div class="icon-container flex items-center justify-center w-12 h-12 bg-gradient-to-br from-primary-500 to-primary-600 rounded-lg">
              <mat-icon class="text-white text-2xl">inbox</mat-icon>
            </div>
            <div class="title-content">
              <h1 class="text-2xl lg:text-3xl font-bold text-gray-900 dark:text-white m-0">Messages</h1>
              <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">{{ (pagination$ | async)?.totalCount || 0 }} total messages</p>
            </div>
          </div>
          
          <div class="status-section flex items-center gap-2" *ngIf="pagination$ | async as pagination">
            <mat-chip class="bg-primary-100 dark:bg-primary-900 text-primary-800 dark:text-primary-200">
              Page {{ pagination.currentPage }} of {{ pagination.totalPages }}
            </mat-chip>
          </div>
        </div>
      </div>

      <!-- Main Content Area -->
      <div class="main-content-area flex-1 flex min-h-0">
        
        <!-- Message List Panel -->
        <div class="message-list-panel flex-shrink-0 w-full lg:w-96 xl:w-1/3 flex flex-col border-r border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          
          <!-- Messages List -->
          <div class="messages-section flex-1 overflow-y-auto">
            <div class="messages-container p-4 space-y-2">
              <div *ngFor="let message of (messages$ | async)?.messages; trackBy: trackByMessageId" 
                   class="message-item cursor-pointer p-4 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 transition-all duration-200"
                   [class.selected]="selectedMessageId === message.id"
                   [class.bg-primary-50]="selectedMessageId === message.id"
                   [class.dark:bg-primary-900]="selectedMessageId === message.id"
                   [class.border-primary-300]="selectedMessageId === message.id"
                   [class.dark:border-primary-600]="selectedMessageId === message.id"
                   (click)="selectMessage(message.id)">
                
                <div class="message-header flex items-start justify-between mb-2">
                  <div class="message-subject font-semibold text-gray-900 dark:text-white truncate flex-1">
                    {{ message.subject || '(No Subject)' }}
                  </div>
                  <div class="message-date text-xs text-gray-500 dark:text-gray-400 ml-2 flex-shrink-0">
                    {{ message.createdAt | date:'short' }}
                  </div>
                </div>
                
                <div class="message-meta text-sm text-gray-600 dark:text-gray-400">
                  <div class="message-from truncate">From: {{ getFromDisplay(message) }}</div>
                  <div class="message-size text-xs text-gray-500 dark:text-gray-500 mt-1">{{ message.size }}</div>
                </div>
              </div>
              
              <!-- Empty State -->
              <div *ngIf="(messages$ | async)?.messages?.length === 0" 
                   class="empty-state flex flex-col items-center justify-center py-16 text-center">
                <div class="empty-icon w-16 h-16 bg-gray-100 dark:bg-gray-700 rounded-full flex items-center justify-center mb-4">
                  <mat-icon class="text-2xl text-gray-400 dark:text-gray-500">inbox</mat-icon>
                </div>
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-2">No messages found</h3>
                <p class="text-gray-600 dark:text-gray-400">When messages arrive, they'll appear here.</p>
              </div>
            </div>
          </div>

          <!-- Pagination Section -->
          <div class="pagination-section flex-shrink-0 p-4 border-t border-gray-200 dark:border-gray-700" 
               *ngIf="pagination$ | async as pagination">
            <div class="pagination-content flex items-center justify-between gap-2">
              <button 
                mat-icon-button
                [disabled]="!pagination.hasPrevious" 
                (click)="goToPage(pagination.currentPage - 1)"
                class="pagination-btn">
                <mat-icon>chevron_left</mat-icon>
              </button>
              
              <div class="page-info text-xs text-gray-600 dark:text-gray-400 text-center flex-1">
                <span class="bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-white px-2 py-1 rounded text-xs">
                  {{ pagination.start + 1 }}-{{ Math.min(pagination.start + pagination.limit, pagination.totalCount) }} 
                  of {{ pagination.totalCount }}
                </span>
              </div>
              
              <button 
                mat-icon-button
                [disabled]="!pagination.hasNext" 
                (click)="goToPage(pagination.currentPage + 1)"
                class="pagination-btn">
                <mat-icon>chevron_right</mat-icon>
              </button>
            </div>
          </div>
        </div>

        <!-- Message Detail Panel -->
        <div class="message-detail-panel flex-1 flex flex-col bg-gray-50 dark:bg-gray-900">
          
          <!-- No Selection State -->
          <div *ngIf="!selectedMessageId" class="no-selection-state flex flex-col items-center justify-center h-full p-8 text-center">
            <div class="icon-container w-24 h-24 bg-gray-100 dark:bg-gray-700 rounded-full flex items-center justify-center mb-6">
              <mat-icon class="text-4xl text-gray-400 dark:text-gray-500">email</mat-icon>
            </div>
            <h2 class="text-xl font-semibold text-gray-900 dark:text-white mb-2">Select a message</h2>
            <p class="text-gray-600 dark:text-gray-400">Choose a message from the list to view its details.</p>
          </div>

          <!-- Message Detail Content -->
          <div *ngIf="selectedMessage$ | async as selectedMessage" class="message-detail-content flex-1 flex flex-col">
            
            <!-- Message Header -->
            <div class="message-detail-header flex-shrink-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-4 lg:p-6">
              <div class="header-content flex items-center gap-4">
                <div class="subject-section flex-1 min-w-0">
                  <h1 class="message-title text-xl lg:text-2xl font-semibold text-gray-800 dark:text-white truncate m-0">
                    {{ selectedMessage.subject || '(No Subject)' }}
                  </h1>
                  <p class="message-date text-sm text-gray-600 dark:text-gray-400 mt-1">
                    {{ selectedMessage.createdAt | date:'full' }}
                  </p>
                </div>
                
                <div class="action-section flex-shrink-0 flex items-center gap-2">
                  <button 
                    mat-raised-button 
                    color="accent" 
                    (click)="downloadRaw(selectedMessage.id)" 
                    class="download-btn flex items-center gap-2">
                    <mat-icon>download</mat-icon>
                    <span class="hidden sm:inline">Download Raw</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Message Content -->
            <div class="message-detail-body flex-1 overflow-y-auto p-4 lg:p-6">
              <div class="content-grid flex flex-col xl:flex-row gap-6 max-w-7xl mx-auto">
                
                <!-- Main Content Column -->
                <div class="main-column flex-1 flex flex-col gap-6">
                  
                  <!-- Message Body Card -->
                  <mat-card class="message-body-card bg-white dark:bg-gray-800">
                    <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                      <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-500 rounded-full">
                        <mat-icon class="text-white text-2xl">article</mat-icon>
                      </div>
                      <div class="header-text">
                        <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Message Content</h2>
                        <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">Email body and content</p>
                      </div>
                    </div>
                    
                    <div class="card-content p-6">
                      <div class="message-body leading-relaxed min-h-32 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border-l-4 border-l-primary-400" 
                           [innerHTML]="(selectedMessage.htmlBody || selectedMessage.textBody) | cidTransform:selectedMessage.id">
                      </div>
                    </div>
                  </mat-card>

                  <!-- Attachments Card -->
                  <mat-card *ngIf="selectedMessage.sections?.length" 
                            class="attachments-card bg-white dark:bg-gray-800">
                    <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                      <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-orange-500 to-red-500 rounded-full">
                        <mat-icon class="text-white text-2xl">attach_file</mat-icon>
                      </div>
                      <div class="header-text">
                        <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Attachments</h2>
                        <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">{{ selectedMessage.sections.length }} attachment(s)</p>
                      </div>
                    </div>
                    
                    <div class="card-content p-6">
                      <div class="attachments-grid flex flex-col gap-3">
                        <div *ngFor="let section of selectedMessage.sections" class="attachment-item">
                          <div *ngIf="section.id" 
                               class="attachment-content flex items-center gap-4 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700">
                            <div class="attachment-icon flex items-center justify-center w-10 h-10 bg-blue-100 dark:bg-blue-900 rounded-full flex-shrink-0">
                              <mat-icon class="text-blue-600 dark:text-blue-400">insert_drive_file</mat-icon>
                            </div>
                            
                            <div class="attachment-info flex-1 min-w-0">
                              <h4 class="attachment-name font-semibold text-gray-800 dark:text-white truncate">
                                {{ section.fileName || section.mediaType }}
                              </h4>
                              <p class="attachment-type text-gray-600 dark:text-gray-400 text-sm">{{ section.mediaType }}</p>
                            </div>
                            
                            <button 
                              mat-raised-button 
                              color="primary"
                              (click)="downloadSection(selectedMessage.id, section.id!)"
                              class="download-attachment-btn flex items-center gap-2 flex-shrink-0">
                              <mat-icon class="text-sm">download</mat-icon>
                              <span class="hidden sm:inline">Download</span>
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </mat-card>
                </div>
                
                <!-- Sidebar Column -->
                <div class="sidebar-column flex-shrink-0 w-full xl:w-80">
                  <mat-card class="message-info-card bg-white dark:bg-gray-800">
                    <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                      <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-primary-500 to-accent-500 rounded-full">
                        <mat-icon class="text-white text-2xl">email</mat-icon>
                      </div>
                      <div class="header-text">
                        <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Message Details</h2>
                        <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">Sender and recipient information</p>
                      </div>
                    </div>
                    
                    <div class="card-content p-6">
                      <div class="info-grid flex flex-col gap-4">
                        
                        <!-- From Section -->
                        <div class="info-item flex items-start gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                          <div class="info-icon flex items-center justify-center w-8 h-8 bg-blue-100 dark:bg-blue-900 rounded-full flex-shrink-0">
                            <mat-icon class="text-blue-600 dark:text-blue-400 text-sm">person</mat-icon>
                          </div>
                          <div class="info-content flex-1 min-w-0">
                            <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">From</h4>
                            <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ selectedMessage.from | emailList }}</p>
                          </div>
                        </div>
                        
                        <!-- To Section -->
                        <div class="info-item flex items-start gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                          <div class="info-icon flex items-center justify-center w-8 h-8 bg-green-100 dark:bg-green-900 rounded-full flex-shrink-0">
                            <mat-icon class="text-green-600 dark:text-green-400 text-sm">people</mat-icon>
                          </div>
                          <div class="info-content flex-1 min-w-0">
                            <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">To</h4>
                            <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ selectedMessage.to | emailList }}</p>
                          </div>
                        </div>
                        
                        <!-- CC Section -->
                        <div *ngIf="selectedMessage.cc?.length" class="info-item flex items-start gap-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                          <div class="info-icon flex items-center justify-center w-8 h-8 bg-yellow-100 dark:bg-yellow-900 rounded-full flex-shrink-0">
                            <mat-icon class="text-yellow-600 dark:text-yellow-400 text-sm">people_outline</mat-icon>
                          </div>
                          <div class="info-content flex-1 min-w-0">
                            <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">CC</h4>
                            <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ selectedMessage.cc | emailList }}</p>
                          </div>
                        </div>
                        
                        <!-- BCC Section -->
                        <div *ngIf="selectedMessage.bCc?.length" class="info-item flex items-start gap-3 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                          <div class="info-icon flex items-center justify-center w-8 h-8 bg-red-100 dark:bg-red-900 rounded-full flex-shrink-0">
                            <mat-icon class="text-red-600 dark:text-red-400 text-sm">visibility_off</mat-icon>
                          </div>
                          <div class="info-content flex-1 min-w-0">
                            <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">BCC</h4>
                            <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ selectedMessage.bCc | emailList }}</p>
                          </div>
                        </div>
                      </div>
                    </div>
                  </mat-card>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }

    .email-client-container {
      height: 100vh;
    }

    .main-content-area {
      min-height: 0;
    }

    .message-list-panel {
      min-width: 300px;
      max-width: 500px;
    }

    .messages-section {
      min-height: 0;
    }

    .messages-container {
      overflow-y: auto;
      scrollbar-width: thin;
      scrollbar-color: #cbd5e1 #f1f5f9;
    }

    .messages-container::-webkit-scrollbar {
      width: 6px;
    }

    .messages-container::-webkit-scrollbar-track {
      @apply bg-gray-100 dark:bg-gray-800;
    }

    .messages-container::-webkit-scrollbar-thumb {
      @apply bg-gray-300 dark:bg-gray-600 rounded-md;
    }

    .message-item {
      @apply transition-all duration-200;
    }

    .message-item:hover {
      @apply shadow-sm;
    }

    .message-item.selected {
      @apply shadow-md;
    }

    .message-detail-body {
      overflow-y: auto;
      scrollbar-width: thin;
      scrollbar-color: #cbd5e1 #f1f5f9;
    }

    .message-detail-body::-webkit-scrollbar {
      width: 8px;
    }

    .message-detail-body::-webkit-scrollbar-track {
      @apply bg-gray-100 dark:bg-gray-800;
    }

    .message-detail-body::-webkit-scrollbar-thumb {
      @apply bg-gray-300 dark:bg-gray-600 rounded-md;
    }

    .message-body {
      @apply min-h-32;
    }

    .message-body img {
      @apply max-w-full h-auto rounded-lg shadow-sm;
    }

    .message-body p {
      @apply mb-4 last:mb-0;
    }

    .message-body a {
      @apply text-primary-600 dark:text-primary-400 hover:text-primary-800 dark:hover:text-primary-300 underline;
    }

    .attachment-content:hover {
      @apply bg-gray-100 dark:bg-gray-800 border-gray-300 dark:border-gray-600;
    }

    .info-item {
      @apply transition-all duration-200 hover:shadow-sm;
    }

    .pagination-btn:disabled {
      @apply opacity-50 cursor-not-allowed;
    }

    @media (max-width: 1024px) {
      .main-content-area {
        @apply flex-col;
      }
      
      .message-list-panel {
        @apply w-full max-w-none border-r-0 border-b;
        max-height: 40vh;
      }
      
      .message-detail-panel {
        @apply flex-1;
      }

      .content-grid {
        @apply flex-col;
      }
      
      .sidebar-column {
        @apply w-full;
      }
    }

    @media (max-width: 640px) {
      .header-content,
      .message-detail-header .header-content {
        @apply p-4;
      }
      
      .content-grid {
        @apply p-4;
      }
      
      .download-btn span,
      .download-attachment-btn span {
        @apply hidden;
      }

      .message-list-panel {
        max-height: 50vh;
      }
    }
  `]
})
export class MessageListComponent {
  messages$: Observable<MessageResponse>;
  pagination$: Observable<PaginationInfo>;
  selectedMessage$: Observable<MessageDetail | null>;
  
  private selectedMessageId$ = new BehaviorSubject<string | null>(null);
  selectedMessageId: string | null = null;
  
  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageRepository: MessageRepository
  ) {
    this.messages$ = this.route.data.pipe(
      map(data => data['messages'])
    );

    this.pagination$ = combineLatest([
      this.route.data,
      this.route.queryParams
    ]).pipe(
      map(([data, queryParams]) => {
        const messages = data['messages'] as MessageResponse;
        const limit = parseInt(queryParams['limit'] || '10', 10);
        const start = parseInt(queryParams['start'] || '0', 10);
        const currentPage = Math.floor(start / limit) + 1;
        const totalPages = Math.ceil(messages.totalMessageCount / limit);

        return {
          currentPage,
          totalPages,
          limit,
          start,
          totalCount: messages.totalMessageCount,
          hasNext: start + limit < messages.totalMessageCount,
          hasPrevious: start > 0
        };
      })
    );

    this.selectedMessage$ = this.selectedMessageId$.pipe(
      switchMap(messageId => {
        if (!messageId) {
          return of(null);
        }
        return this.messageRepository.getMessage(messageId);
      })
    );
  }

  trackByMessageId(index: number, message: Message): string {
    return message.id;
  }

  selectMessage(messageId: string): void {
    this.selectedMessageId = messageId;
    this.selectedMessageId$.next(messageId);
  }

  getFromDisplay(message: Message): string {
    return 'Sender';
  }

  downloadRaw(messageId: string): void {
    this.messageRepository.downloadRawMessage(messageId);
  }

  downloadSection(messageId: string, contentId: string): void {
    this.messageRepository.downloadSectionByContentId(messageId, contentId);
  }

  goToPage(page: number) {
    this.pagination$.subscribe(pagination => {
      const newStart = (page - 1) * pagination.limit;
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {
          start: newStart,
          limit: pagination.limit
        },
        queryParamsHandling: 'merge'
      });
    }).unsubscribe();
  }
} 