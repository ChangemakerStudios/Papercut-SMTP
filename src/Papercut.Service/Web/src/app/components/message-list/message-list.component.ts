import { Component, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Observable, map, combineLatest, BehaviorSubject, switchMap, of, tap, take, finalize } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Message, MessageResponse, MessageRepository, MessageDetail } from '../../services/message.repository';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { MessageListItemComponent } from './message-list-item.component';
import { MessageDetailsComponent } from './message-details.component';
import { MessageService } from '../../services/message.service';

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
    MatTabsModule,
    MatExpansionModule,
    MatTooltipModule,
    ScrollingModule,
    MessageListItemComponent,
    MessageDetailsComponent
  ],
  template: `
    <div class="message-list-container">
      <!-- Message List Panel -->
      <div class="message-list-panel">
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
        <cdk-virtual-scroll-viewport [itemSize]="itemSize">
          <app-message-list-item
            *cdkVirtualFor="let message of allMessages; trackBy: trackByMessageId"
            [message]="message"
            [selected]="message.id === selectedMessageId"
            (select)="selectMessage(message.id)">
          </app-message-list-item>
          
          <!-- Loading indicator for infinite scroll -->
          <div *ngIf="isLoadingMore" class="loading-more-indicator">
            <mat-spinner diameter="24" strokeWidth="3"></mat-spinner>
            <span>Loading more messages...</span>
          </div>
        </cdk-virtual-scroll-viewport>
      </div>

      <!-- Message Detail Panel -->
      <div class="message-detail-panel">
        <app-message-details 
          [message]="selectedMessage$ | async">
        </app-message-details>
      </div>
    </div>
  `,
  styles: [``]
})
export class MessageListComponent implements OnDestroy {
  messages$: Observable<MessageResponse> = this.route.data.pipe(
    map(data => data['messages'])
  );
  pagination$: Observable<PaginationInfo>;
  selectedMessage$: Observable<MessageDetail | null>;
  
  private selectedMessageId$ = new BehaviorSubject<string | null>(null);
  selectedMessageId: string | null = null;
  isLoadingMessage = false;
  private loadingTimeout: any = null;
  
  // Tab and attachment management
  selectedTabIndex = 0;
  showAttachments = false;
  
  // Virtual scroll settings  
  itemSize = 50; // Height of each message item in pixels
  viewportHeight = 0;
  allMessages: Message[] = [];
  private currentPage = 1;
  isLoadingMore = false;
  private hasMorePages = true;
  private readonly pageSize = 10; // Match backend's default limit
  
  Math = Math;

  @ViewChild(CdkVirtualScrollViewport) viewport!: CdkVirtualScrollViewport;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    this.messages$.subscribe((response: MessageResponse) => {
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

    // Initialize selected message observable
    this.selectedMessage$ = this.selectedMessageId$.pipe(
      switchMap(id => {
        if (!id) return of(null);
        this.isLoadingMessage = true;
        return this.messageService.getMessage(id).pipe(
          finalize(() => {
            this.isLoadingMessage = false;
          })
        );
      })
    );

    this.route.data.subscribe(data => {
      console.log('Route data:', data);
    });
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
    ).subscribe(response => {
      this.allMessages = [...this.allMessages, ...response.messages];
      this.currentPage = nextPage;
      this.hasMorePages = this.allMessages.length < response.totalMessageCount;
    });
  }

  trackByMessageId(index: number, message: Message): string {
    return message.id;
  }

  isSelectedMessage(messageId: string): boolean {
    return this.selectedMessageId === messageId;
  }

  selectMessage(messageId: string): void {
    console.log('Selecting message:', messageId);
    this.selectedMessageId = messageId;
    this.selectedMessageId$.next(messageId);
  }

  getFromDisplay(message: Message): string {
    if (!message.from?.length) return 'Unknown';
    const sender = message.from[0];
    return sender.name && sender.name !== sender.address 
      ? `${sender.name} <${sender.address}>`
      : sender.address;
  }

  toggleAttachments(): void {
    this.showAttachments = !this.showAttachments;
  }

  getPriorityDisplay(message: MessageDetail): string {
    // Return priority or default to 'Normal'
    return 'Normal'; // You can implement priority logic here
  }

  getRawMessageContent(message: MessageDetail): string {
    // Return raw message content for Raw View tab
    return message.textBody || message.htmlBody || 'No raw content available';
  }

  getMessageSections(message: MessageDetail): any[] {
    // Return message sections
    return message.sections || [];
  }

  downloadRaw(messageId: string): void {
    // Implement download logic here or in MessageService if needed
  }

  downloadSection(messageId: string, contentId: string): void {
    // Implement download logic here or in MessageService if needed
  }

  ngOnDestroy() {
    // Clean up any pending loading timeout
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
      this.loadingTimeout = null;
    }
  }
} 