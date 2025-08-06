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

import { ResizerComponent } from '../resizer/resizer.component';
import { MessageListItemComponent } from './message-list-item.component';

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
    ResizerComponent,
    MessageListItemComponent
  ],
  template: `
    <div class="flex h-full bg-gray-100 dark:bg-gray-900 transition-colors duration-300" 
         [class.dragging]="isDragging">
      <!-- Message List Panel -->
      <div class="border-r border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 flex flex-col" 
           [ngStyle]="{'flex': '0 0 ' + messageListWidth + 'px'}">
        <!-- Message List Header -->
        <!-- <div class="message-list-header">
          <div class="message-list-title">
            <mat-icon>inbox</mat-icon>
            Messages
          </div>
          <div class="message-count" *ngIf="pagination$ | async as pagination">
            {{ pagination.totalCount }} total
            <span *ngIf="isLoadingMore" class="ml-2">
              <mat-spinner diameter="12" strokeWidth="2"></mat-spinner>
            </span>
          </div>
        </div> -->

        <!-- Virtual Scroll List -->
        <cdk-virtual-scroll-viewport 
          [itemSize]="itemSize" 
          [minBufferPx]="200"
          [maxBufferPx]="400"
          (scrolledIndexChange)="onScroll()" 
          class="w-full overflow-hidden virtual-scroll-container">
          <app-message-list-item 
            *cdkVirtualFor="let message of allMessages; trackBy: trackByMessageId; templateCacheSize: 0"
            [message]="message"
            [selected]="message.id === selectedMessageId"
            (select)="selectMessage(message.id!)"
            class="block w-full">
          </app-message-list-item>
        </cdk-virtual-scroll-viewport>
        
        <!-- Loading indicator for infinite scroll - outside virtual scroll -->
        <div *ngIf="isLoadingMore" class="flex items-center justify-center p-4 gap-2 text-gray-600 dark:text-gray-300 bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700">
          <mat-spinner diameter="24" strokeWidth="3"></mat-spinner>
          <span>Loading more messages...</span>
        </div>
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
      <div class="flex-1 bg-white dark:bg-gray-800 flex flex-col min-w-0">
        <router-outlet></router-outlet>
        
        <div *ngIf="!selectedMessageId" class="flex-1 flex flex-col items-center justify-center p-8">
          <mat-icon class="text-6xl mb-4 text-gray-400 dark:text-gray-500 !w-auto !h-auto">email</mat-icon>
          <h3 class="text-xl font-medium mb-2 text-gray-700 dark:text-gray-300">No message selected</h3>
          <p class="text-gray-600 dark:text-gray-400">Select a message from the list to view its contents</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* Virtual Scroll Container with flexible height */
    .virtual-scroll-container {
      flex: 1;
      min-height: 0;
      height: 100%;
      max-height: 100%;
      overflow: auto;
    }

    /* CDK Virtual Scroll content wrapper width constraint */
    ::ng-deep cdk-virtual-scroll-viewport .cdk-virtual-scroll-content-wrapper {
      width: 100% !important;
      max-width: 100% !important;
      overflow: hidden !important;
      box-sizing: border-box !important;
    }

    ::ng-deep cdk-virtual-scroll-viewport .cdk-virtual-scroll-content-wrapper > * {
      width: 100% !important;
      max-width: 100% !important;
      box-sizing: border-box !important;
    }

    /* Prevent over-scrolling in virtual scroll */
    ::ng-deep cdk-virtual-scroll-viewport {
      overflow-anchor: none;
      will-change: transform;
    }

    ::ng-deep cdk-virtual-scroll-viewport .cdk-virtual-scroll-spacer {
      pointer-events: none;
    }

    /* Dragging state */
    .dragging {
      user-select: none;
    }

    .dragging .cursor-pointer {
      pointer-events: none;
    }

    /* Responsive design */
    @media (max-width: 768px) {
      .message-list-panel {
        flex: 0 0 100% !important;
      }
      
      .message-detail-panel {
        display: none;
      }
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
    
    // Only trigger loading if we have items and are near the end
    if (total > 0) {
      const threshold = Math.min(3, Math.max(1, Math.floor(total * 0.1))); // 10% of total or 3 items, whichever is smaller
      if (end >= total - threshold && !this.isLoadingMore && this.hasMorePages) {
        this.loadMoreMessages();
      }
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