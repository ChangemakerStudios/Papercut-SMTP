import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Observable, finalize, filter, Subject, takeUntil } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MessageService } from '../../services/message.service';
import { GetMessagesResponse, RefDto, DetailDto } from '../../models';

import { ResizerComponent } from '../resizer/resizer.component';
import { MessageListItemComponent } from './message-list-item.component';
import { PaginationComponent } from '../pagination/pagination.component';

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
    MessageListItemComponent,
    PaginationComponent
  ],
  template: `
    <div class="flex h-full bg-gray-100 dark:bg-gray-900 transition-colors duration-300" 
         [class.dragging]="isDragging">
      <!-- Message List Panel -->
      <div class="border-r border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 flex flex-col message-list-panel" 
           [ngStyle]="{'flex': '0 0 ' + messageListWidth + 'px'}">
        <!-- Paginated List -->
        <div class="w-full overflow-auto virtual-scroll-container flex-1">
          <app-message-list-item
            *ngFor="let message of allMessages; trackBy: trackByMessageId"
            [message]="message"
            [selected]="message.id === selectedMessageId"
            [isLoading]="isLoading"
            [isLoadingDetail]="loadingMessageId === message.id"
            (select)="selectMessage(message.id!)"
            class="block w-full">
          </app-message-list-item>
        </div>
        
        <!-- Pagination Bar -->
        <div class="w-full min-w-0">
          <app-pagination
            [pageSize]="pageSize"
            [pageStart]="pageStart"
            [currentPage]="currentPage"
            [totalPages]="totalPages"
            [totalCount]="totalCount"
            [pageSizeOptions]="pageSizeOptions"
            [isLoading]="isLoading"
            (pageSizeChange)="onPageSizeChange($event)"
            (pageChange)="goToPage($event)">
          </app-pagination>
        </div>
      </div>

      <!-- Resizer Handle -->
      <div class="flex-shrink-0">
        <app-resizer 
          [currentWidth]="messageListWidth"
          [minWidth]="200"
          [maxWidth]="2000"
          [defaultWidth]="400"
          localStorageKey="papercut-message-list-width"
          (widthChange)="onWidthChange($event)"
          (draggingChange)="onDraggingChange($event)">
        </app-resizer>
      </div>

      <!-- Message Detail Panel -->
      <div class="flex-1 bg-white dark:bg-gray-800 flex flex-col min-w-0 relative">
        <!-- Buffer loader overlay -->
        <div *ngIf="isLoadingMessageDetail" class="absolute inset-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm z-10 flex items-center justify-center">
          <div class="flex flex-col items-center gap-3">
            <mat-spinner diameter="40" strokeWidth="4"></mat-spinner>
            <span class="text-sm font-medium text-gray-600 dark:text-gray-300">Loading message...</span>
          </div>
        </div>
        
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
    /* Scroll Container with flexible height */
    .virtual-scroll-container {
      flex: 1;
      min-height: 0;
      height: 100%;
      max-height: 100%;
      overflow: auto;
    }

    /* Dragging state */
    .dragging {
      user-select: none;
    }

    .dragging .cursor-pointer {
      pointer-events: none;
    }

    /* Ensure message list panel respects width constraints */
    .message-list-panel {
      min-width: 0;
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
  // Observables are no longer used to drive the list directly; we imperatively load a page on query param change
  messages$!: Observable<GetMessagesResponse>;
  
  selectedMessageId: string | null = null;
  private loadingTimeout: any = null;
  private destroy$ = new Subject<void>();
  
  // Pagination state
  allMessages: RefDto[] = [];
  pageSize = 10;
  pageStart = 0;
  currentPage = 1;
  totalPages = 1;
  totalCount = 0;
  pageSizeOptions: number[] = [10, 25, 50, 100];

  // Resizer properties
  messageListWidth = 400; // Default width
  isDragging = false;
  isLoading = false;
  isLoadingMessageDetail = false;
  loadingMessageId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    // Load current page when query params change
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const limit = parseInt(params['limit'] || '10', 10);
        const start = parseInt(params['start'] || '0', 10);
        this.pageSize = limit;
        this.pageStart = start;
        this.currentPage = Math.floor(start / limit) + 1;
        this.loadPage(limit, start);
      });

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

  private loadPage(limit: number, start: number): void {
    this.isLoading = true;
    this.messageService.getMessages(limit, start)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe((response: GetMessagesResponse) => {
        this.allMessages = response.messages;
        this.totalCount = response.totalMessageCount;
        this.totalPages = Math.max(1, Math.ceil(this.totalCount / this.pageSize));
      });
  }

  trackByMessageId(index: number, message: RefDto): string {
    return message.id || index.toString();
  }

  selectMessage(messageId: string): void {
    console.log('Selecting message:', messageId);
    this.loadingMessageId = messageId;
    this.isLoadingMessageDetail = true;
    this.selectedMessageId = messageId;
    this.router.navigate(['message', messageId], { 
      relativeTo: this.route,
      queryParamsHandling: 'preserve'
    });
    
    // Clear loading state after a short delay (the message detail component should handle its own loading)
    setTimeout(() => {
      this.loadingMessageId = null;
      this.isLoadingMessageDetail = false;
    }, 500);
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

  // Pagination actions
  onPageSizeChange(size: number): void {
    this.pageSize = Number(size) || 10;
    this.goToPage(1);
  }

  goToPage(page: number): void {
    const safePage = Math.min(Math.max(1, page), this.totalPages || 1);
    const start = (safePage - 1) * this.pageSize;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { limit: this.pageSize, start },
      queryParamsHandling: 'merge'
    });
  }

  onDraggingChange(isDragging: boolean): void {
    this.isDragging = isDragging;
  }
} 