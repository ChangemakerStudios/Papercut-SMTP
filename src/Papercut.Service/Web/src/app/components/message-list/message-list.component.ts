import { Component, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Observable, map, combineLatest, finalize, filter, Subject, takeUntil } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { MessageService } from '../../services/message.service';
import { GetMessagesResponse, RefDto, DetailDto } from '../../models';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { ResizerComponent } from '../resizer/resizer.component';

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
    MatProgressSpinnerModule,
    ScrollingModule,
    EmailListPipe,
    CidTransformPipe,
    ResizerComponent
  ],
  template: `
    <div class="message-list-container" [class.dragging]="isDragging">
      <!-- Message List Panel -->
      <div class="message-list-panel" [ngStyle]="{'flex': '0 0 ' + messageListWidth + 'px'}">
        <!-- Message List Header -->
        <div class="message-list-header">
          <h2 class="message-list-title">
            <mat-icon style="margin-right: 0.5rem; vertical-align: middle;">inbox</mat-icon>
            Messages
          </h2>
          <p class="message-count" *ngIf="pagination$ | async as pagination">
            {{ pagination.totalCount }} total messages
            <span *ngIf="isLoadingMore" style="margin-left: 0.5rem;">
              <mat-spinner diameter="16" strokeWidth="2"></mat-spinner>
            </span>
          </p>
        </div>

        <!-- Virtual Scroll List -->
        <cdk-virtual-scroll-viewport [itemSize]="itemSize" (scrolledIndexChange)="onScroll()">
          <div *cdkVirtualFor="let message of allMessages; trackBy: trackByMessageId"
               class="message-item"
               [class.selected]="message.id === selectedMessageId"
               (click)="selectMessage(message.id!)">
            <div class="message-from">{{ getFromDisplay(message) }}</div>
            <div class="message-subject">{{ message.subject || '(No Subject)' }}</div>
            <div class="message-meta">
              <span>{{ message.createdAt | date:'short' }}</span>
              <span>{{ message.size }}</span>
            </div>
          </div>
          
          <!-- Loading indicator for infinite scroll -->
          <div *ngIf="isLoadingMore" class="loading-more-indicator">
            <mat-spinner diameter="24" strokeWidth="3"></mat-spinner>
            <span>Loading more messages...</span>
          </div>
        </cdk-virtual-scroll-viewport>
      </div>

      <!-- Resizer Handle -->
      <app-resizer 
        [currentWidth]="messageListWidth"
        [minWidth]="200"
        [maxWidth]="2000"
        [defaultWidth]="400"
        localStorageKey="papercut-message-list-width"
        (widthChange)="onWidthChange($event)"
        (draggingChange)="onDraggingChange($event)">
      </app-resizer>

      <!-- Message Detail Panel -->
      <div class="message-detail-panel">
        <router-outlet></router-outlet>
        
        <div *ngIf="!selectedMessageId" class="no-message">
          <mat-icon>email</mat-icon>
          <h3>No message selected</h3>
          <p>Select a message from the list to view its contents</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .message-list-container {
      display: flex;
      height: 100vh;
      background-color: #f5f5f5;
    }

    .message-list-panel {
      border-right: 1px solid #e0e0e0;
      background-color: white;
      display: flex;
      flex-direction: column;
    }

    .message-list-header {
      padding: 1rem;
      border-bottom: 1px solid #e0e0e0;
      background-color: #fafafa;
    }

    .message-list-title {
      margin: 0 0 0.5rem 0;
      font-size: 1.2rem;
      font-weight: 600;
      color: #333;
    }

    .message-count {
      margin: 0;
      font-size: 0.9rem;
      color: #666;
    }

    cdk-virtual-scroll-viewport {
      flex: 1;
      height: 100%;
    }

    .message-item {
      padding: 0.75rem 1rem;
      border-bottom: 1px solid #f0f0f0;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .message-item:hover {
      background-color: #f8f9fa;
    }

    .message-item.selected {
      background-color: #e3f2fd;
      border-left: 3px solid #2196f3;
    }

    .message-from {
      font-weight: 600;
      color: #333;
      margin-bottom: 0.25rem;
    }

    .message-subject {
      color: #555;
      margin-bottom: 0.25rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .message-meta {
      display: flex;
      justify-content: space-between;
      font-size: 0.8rem;
      color: #888;
    }

    .loading-more-indicator {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1rem;
      gap: 0.5rem;
      color: #666;
    }

    .loading-message {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      gap: 0.5rem;
      color: #666;
      background-color: #f8f9fa;
      border-radius: 8px;
      margin: 1rem;
    }



    .message-detail-panel {
      flex: 1;
      background-color: white;
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .message-details {
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .message-details-header {
      padding: 1.5rem;
      border-bottom: 1px solid #e0e0e0;
      background-color: #fafafa;
    }

    .message-subject {
      font-weight: 600;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .message-from {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #666;
      margin-bottom: 0.5rem;
    }

    .message-meta {
      display: flex;
      gap: 1rem;
    }

    .message-meta-item {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      font-size: 0.9rem;
      color: #888;
    }

    .message-content {
      flex: 1;
      padding: 1.5rem;
      overflow-y: auto;
    }

    .message-body {
      line-height: 1.6;
      color: #333;
      margin-bottom: 1.5rem;
    }

    .attachments {
      border-top: 1px solid #e0e0e0;
      padding-top: 1rem;
    }

    .attachments h4 {
      margin: 0 0 0.5rem 0;
      color: #333;
    }

    .attachment-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem;
      background-color: #f8f9fa;
      border-radius: 4px;
      margin-bottom: 0.5rem;
    }

    .attachment-item mat-icon {
      color: #666;
    }

    .attachment-item span {
      flex: 1;
      font-size: 0.9rem;
    }

    .no-message {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #999;
      text-align: center;
    }

    .no-message mat-icon {
      font-size: 4rem;
      width: 4rem;
      height: 4rem;
      margin-bottom: 1rem;
    }

    .no-message h3 {
      margin: 0 0 0.5rem 0;
      font-weight: 500;
    }

    .no-message p {
      margin: 0;
      font-size: 0.9rem;
    }

    /* Dark theme support */
    :host-context(body[data-theme="dark"]) .message-list-container {
      background-color: #121212;
    }

    :host-context(body[data-theme="dark"]) .message-list-panel,
    :host-context(body[data-theme="dark"]) .message-detail-panel {
      background-color: #1e1e1e;
      border-color: #333;
    }

    :host-context(body[data-theme="dark"]) .message-list-header,
    :host-context(body[data-theme="dark"]) .message-details-header {
      background-color: #2d2d2d;
      border-color: #333;
    }

    :host-context(body[data-theme="dark"]) .message-item {
      border-color: #333;
    }

    :host-context(body[data-theme="dark"]) .message-item:hover {
      background-color: #2d2d2d;
    }

    :host-context(body[data-theme="dark"]) .message-item.selected {
      background-color: #1565c0;
    }

    :host-context(body[data-theme="dark"]) .message-from,
    :host-context(body[data-theme="dark"]) .message-subject,
    :host-context(body[data-theme="dark"]) .message-list-title {
      color: #ffffff;
    }

    :host-context(body[data-theme="dark"]) .message-body {
      color: #ffffff;
    }

    :host-context(body[data-theme="dark"]) .attachment-item {
      background-color: #2d2d2d;
    }

    /* Responsive design */
    @media (max-width: 768px) {
      .message-list-panel {
        flex: 0 0 100%;
      }
      
      .message-detail-panel {
        display: none;
      }
      
      .message-item.selected + .message-detail-panel {
        display: flex;
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 1000;
      }
    }

    // Responsive design - adjust layout on mobile devices
    @media (max-width: 768px) {
      .message-list-panel {
        flex: 0 0 100% !important;
      }
      
      .message-detail-panel {
        display: none;
      }
    }

    // Prevent text selection during dragging
    .message-list-container.dragging {
      user-select: none;
    }
  `]
})
export class MessageListComponent implements OnDestroy {
  messages$: Observable<GetMessagesResponse> = this.route.data.pipe(
    map(data => data['messages'])
  );
  pagination$: Observable<PaginationInfo>;
  
  selectedMessageId: string | null = null;
  private loadingTimeout: any = null;
  private destroy$ = new Subject<void>();
  
  // Virtual scroll settings  
  itemSize = 80; // Height of each message item in pixels
  allMessages: RefDto[] = [];
  private currentPage = 1;
  isLoadingMore = false;
  private hasMorePages = true;
  private readonly pageSize = 10;

  // Resizer properties
  messageListWidth = 400; // Default width
  isDragging = false;

  @ViewChild(CdkVirtualScrollViewport) viewport!: CdkVirtualScrollViewport;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    this.messages$.pipe(
      takeUntil(this.destroy$)
    ).subscribe((response: GetMessagesResponse) => {
      console.log('Messages loaded:', response);
      this.allMessages = response.messages;
      this.hasMorePages = response.messages.length < response.totalMessageCount;
      this.currentPage = 1;
    });

    this.pagination$ = combineLatest([
      this.route.data,
      this.route.queryParams
    ]).pipe(
      map(([data, queryParams]) => {
        const messages = data['messages'] as GetMessagesResponse;
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

    // Listen for route changes to detect when a message is selected via URL
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.updateSelectedMessageFromUrl();
    });

    // Set initial selected message from URL
    this.updateSelectedMessageFromUrl();

    // Note: Resizer component handles localStorage loading automatically
  }

  private updateSelectedMessageFromUrl(): void {
    // Check if we're on a child route with a message ID
    let currentRoute = this.route.firstChild;
    if (currentRoute && currentRoute.snapshot && currentRoute.snapshot.params['id']) {
      const messageId = currentRoute.snapshot.params['id'];
      console.log('Message ID from URL:', messageId);
      this.selectedMessageId = messageId;
    } else {
      this.selectedMessageId = null;
    }
  }

  // Handle virtual scroll events
  onScroll(): void {
    if (!this.viewport) return;
    
    const end = this.viewport.getRenderedRange().end;
    const total = this.viewport.getDataLength();
    
    if (end === total && !this.isLoadingMore && this.hasMorePages) {
      this.loadMoreMessages();
    }
  }

  private loadMoreMessages(): void {
    if (this.isLoadingMore) return;
    
    this.isLoadingMore = true;
    const nextPage = this.currentPage + 1;
    const start = (nextPage - 1) * this.pageSize;

    this.messageService.getMessages(this.pageSize, start).pipe(
      finalize(() => {
        this.isLoadingMore = false;
      })
    ).subscribe((response: GetMessagesResponse) => {
      this.allMessages = [...this.allMessages, ...response.messages];
      this.currentPage = nextPage;
      this.hasMorePages = this.allMessages.length < response.totalMessageCount;
    });
  }

  trackByMessageId(index: number, message: RefDto): string {
    return message.id || index.toString();
  }

  selectMessage(messageId: string): void {
    console.log('Selecting message:', messageId);
    this.selectedMessageId = messageId;
    this.router.navigate(['message', messageId], { relativeTo: this.route });
  }

  getFromDisplay(message: RefDto): string {
    // For RefDto, we don't have detailed from information, so we'll just show a placeholder
    return 'Sender'; // This could be enhanced when the API provides more detailed ref data
  }

  downloadSection(messageId: string, contentId: string): void {
    this.messageService.downloadSectionByContentId(messageId, contentId);
  }

  ngOnDestroy() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
      this.loadingTimeout = null;
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Resizer event handlers
  onWidthChange(width: number): void {
    this.messageListWidth = width;
  }

  onDraggingChange(isDragging: boolean): void {
    this.isDragging = isDragging;
  }
} 