import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { RouterModule, ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Observable, finalize, filter, skip, Subject, takeUntil } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MessageService } from '../../services/message.service';
import { MessageApiService } from '../../services/message-api.service';
import { SignalRService } from '../../services/signalr.service';
import { ToastNotificationService } from '../../services/toast-notification.service';
import { PlatformNotificationService } from '../../services/platform-notification.service';
import { LoggingService } from '../../services/logging.service';
import { MessageStateService } from '../../services/message-state.service';
import { UserSettingsService } from '../../services/user-settings.service';
import { GetMessagesResponse, RefDto, DetailDto } from '../../models';

import { ResizerComponent } from '../resizer/resizer.component';
import { MessageListItemComponent } from './message-list-item.component';
import { MessageListEmptyStateComponent } from './message-list-empty-state.component';
import { MessageListNoSelectionComponent } from './message-list-no-selection.component';

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatCardModule, 
    MatButtonModule, 

    MatChipsModule,
    MatProgressSpinnerModule,
    ScrollingModule,
    ResizerComponent,
    MessageListItemComponent,
    MessageListEmptyStateComponent,
    MessageListNoSelectionComponent
  ],
  template: `
    <div class="flex h-full bg-gray-100 dark:bg-gray-900 transition-colors duration-300" 
         [class.dragging]="isDragging">
      <!-- Message List Panel -->
      <div class="border-r border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 flex flex-col message-list-panel"
           [ngStyle]="{'flex': '0 0 ' + messageListWidth + 'px'}">
        <!-- Count bar -->
        <div class="list-count-bar">
          <span class="list-count">
            {{ totalCount }} {{ totalCount === 1 ? 'message' : 'messages' }}
          </span>
          <span class="list-loaded" *ngIf="allMessages.length < totalCount">
            {{ allMessages.length }} loaded
          </span>
          <mat-spinner *ngIf="isLoading || isLoadingMore" diameter="12" strokeWidth="2"></mat-spinner>
        </div>

        <!-- No Messages Placeholder -->
        <app-message-list-empty-state *ngIf="!isLoading && allMessages.length === 0"></app-message-list-empty-state>

        <!-- Virtualized list: only the visible rows exist in the DOM, and the
             next chunk loads as it comes into view -->
        <cdk-virtual-scroll-viewport
          *ngIf="allMessages.length > 0"
          [itemSize]="messageRowHeight"
          minBufferPx="600"
          maxBufferPx="1200"
          class="flex-1 w-full"
          (scrolledIndexChange)="onScrolledIndexChange()">
          <app-message-list-item
            *cdkVirtualFor="let message of allMessages; trackBy: trackByMessageId"
            [message]="message"
            [selected]="message.id === selectedMessageId"
            (select)="selectMessage(message.id!)"
            class="block w-full">
          </app-message-list-item>
        </cdk-virtual-scroll-viewport>
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
        <!-- No loader here: the detail pane owns the one loading indicator,
             and it lifts when the body is actually on screen -->
        <router-outlet></router-outlet>
        
        <app-message-list-no-selection *ngIf="!selectedMessageId"></app-message-list-no-selection>
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

    .list-count-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
      padding: 6px 14px;
      border-bottom: 1px solid var(--pc-border);
      background: var(--pc-surface-2);
      font-size: 11.5px;
      color: var(--pc-muted);
    }

    .list-count {
      font-weight: 600;
      color: var(--pc-ink);
    }

    .list-loaded {
      color: var(--pc-faint);
    }

    cdk-virtual-scroll-viewport {
      min-height: 0;
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
export class MessageListComponent implements OnInit, OnDestroy {
  // Observables are no longer used to drive the list directly; we imperatively load a page on query param change
  messages$!: Observable<GetMessagesResponse>;
  
  selectedMessageId: string | null = null;
  private loadingTimeout: any = null;
  private destroy$ = new Subject<void>();

  @ViewChild(CdkVirtualScrollViewport) viewport?: CdkVirtualScrollViewport;

  /** Must match the fixed .msg-item height for virtual scrolling to line up */
  readonly messageRowHeight = 76;

  /** How many messages are fetched per trip to the server */
  private readonly chunkSize = 100;

  allMessages: RefDto[] = [];
  totalCount = 0;
  isLoadingMore = false;

  // Resizer properties
  messageListWidth = 400; // Default width
  isDragging = false;
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService,
    private messageApiService: MessageApiService,
    private signalRService: SignalRService,
    private toastService: ToastNotificationService,
    private platformNotificationService: PlatformNotificationService,
    private loggingService: LoggingService,
    private messageStateService: MessageStateService,
    private userSettingsService: UserSettingsService,
    private dialog: MatDialog
  ) {
    // Reload when the sort order preference changes
    this.userSettingsService.sortOrder$
      .pipe(skip(1), takeUntil(this.destroy$))
      .subscribe(() => this.loadFirstChunk());

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

  ngOnInit(): void {
    this.loadFirstChunk();

    // Start SignalR connection
    this.signalRService.start();

    // Subscribe to new message notifications with error handling
    this.signalRService.newMessage$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newMessage) => {
          if (newMessage) {
            this.loggingService.debug('New message received via SignalR', newMessage);
            try {
              this.handleNewMessage(newMessage);
            } catch (error) {
              this.loggingService.error('Error handling new message', error);
            }
          }
        },
        error: (error) => {
          this.loggingService.error('Error in SignalR new message subscription', error);
        }
      });

    // Subscribe to message list change notifications with error handling
    this.signalRService.messageListChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (changed) => {
          if (changed) {
            this.loggingService.debug('Message list changed via SignalR, refreshing...');
            try {
              this.refreshCurrentPage();
            } catch (error) {
              this.loggingService.error('Error refreshing page after SignalR notification', error);
            }
          }
        },
        error: (error) => {
          this.loggingService.error('Error in SignalR message list change subscription', error);
        }
      });

    // Refresh when toolbar actions (delete, delete all) change the list
    this.messageStateService.refreshRequests$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.refreshCurrentPage());

    // Reload when the sort order preference changes (Options dialog)
    this.userSettingsService.sortOrder$
      .pipe(skip(1), takeUntil(this.destroy$))
      .subscribe(() => this.refreshCurrentPage());

    // Subscribe to connection status
    this.signalRService.isConnected$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (isConnected) => {
          this.loggingService.debug('SignalR connection status', { isConnected });
        },
        error: (error) => {
          this.loggingService.error('Error in SignalR connection status subscription', error);
        }
      });
  }

  private updateSelectedMessageFromUrl(): void {
    // Check if we're on a child route with a message ID
    let currentRoute = this.route.firstChild;
    if (currentRoute && currentRoute.snapshot && currentRoute.snapshot.params['id']) {
      const messageId = currentRoute.snapshot.params['id'];
      this.loggingService.debug('Message ID from URL', { messageId });
      this.selectedMessageId = messageId;
    } else {
      this.selectedMessageId = null;
    }

    this.messageStateService.setCurrentMessageId(this.selectedMessageId);
  }

  /** Loads the first chunk, replacing whatever is in the list. */
  private loadFirstChunk(): void {
    this.isLoading = true;

    this.fetch(0, this.chunkSize)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe(response => {
        this.allMessages = response.messages;
        this.setTotalCount(response.totalMessageCount);
      });
  }

  /** Appends the next chunk as the user scrolls toward the end. */
  private loadNextChunk(): void {
    if (this.isLoadingMore || this.allMessages.length >= this.totalCount) return;

    this.isLoadingMore = true;

    this.fetch(this.allMessages.length, this.chunkSize)
      .pipe(finalize(() => { this.isLoadingMore = false; }))
      .subscribe(response => {
        // guard against duplicates if the list shifted while loading
        const known = new Set(this.allMessages.map(m => m.id));
        const fresh = response.messages.filter(m => !known.has(m.id));

        this.allMessages = [...this.allMessages, ...fresh];
        this.setTotalCount(response.totalMessageCount);
      });
  }

  onScrolledIndexChange(): void {
    const end = this.viewport?.getRenderedRange().end ?? 0;

    // start the next chunk a screenful before the end so scrolling stays smooth
    if (end >= this.allMessages.length - 20) {
      this.loadNextChunk();
    }
  }

  private fetch(start: number, limit: number): Observable<GetMessagesResponse> {
    return this.messageApiService.getMessages(limit, start, this.userSettingsService.getSortOrder());
  }

  private setTotalCount(total: number): void {
    this.totalCount = total;
    this.messageStateService.setTotalCount(total);
  }

  private handleNewMessage(newMessage: RefDto): void {
    // Extract sender and subject information
    const sender = newMessage.from && newMessage.from.length > 0 
      ? newMessage.from[0].name || newMessage.from[0].address || 'Unknown Sender'
      : 'Unknown Sender';
    const subject = newMessage.subject || 'No Subject';

    // Notifications honor the Options "Show new message notifications" setting
    if (this.userSettingsService.areNotificationsEnabled()) {
      this.toastService.showNewMessageToast(
        subject,
        sender,
        newMessage.id!,
        () => this.selectAndViewMessage(newMessage.id!)
      );

      // Show platform notification only if tab is not visible
      this.platformNotificationService.showNewMessageNotificationIfTabHidden(
        subject,
        sender,
        () => this.selectAndViewMessage(newMessage.id!)
      );
    }

    const alreadyListed = this.allMessages.some(msg => msg.id === newMessage.id);

    if (!alreadyListed) {
      // newest first is the default order, so a new message goes on top;
      // ascending puts it at the end, but only once everything is loaded
      if (this.userSettingsService.getSortOrder() === 'asc') {
        if (this.allMessages.length >= this.totalCount) {
          this.allMessages = [...this.allMessages, newMessage];
        }
      } else {
        this.allMessages = [newMessage, ...this.allMessages];
      }

      this.loggingService.debug('Added new message to list', { messageId: newMessage.id });
    }

    this.setTotalCount(this.totalCount + 1);
  }

  /** Reloads everything currently loaded, keeping the user's scroll depth. */
  private refreshCurrentPage(): void {
    const loaded = Math.max(this.chunkSize, this.allMessages.length);

    this.isLoading = true;

    this.fetch(0, loaded)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe(response => {
        this.allMessages = response.messages;
        this.setTotalCount(response.totalMessageCount);
      });
  }

  trackByMessageId(index: number, message: RefDto): string {
    return message.id || index.toString();
  }

  private selectAndViewMessage(messageId: string): void {
    // A dialog (log viewer, options, rules) would cover the navigation —
    // viewing a message from a notification should bring it into view
    this.dialog.closeAll();

    // Navigate to the message detail (route is /message/:id)
    this.router.navigate(['/message', messageId], { queryParamsHandling: 'preserve' });
  }

  selectMessage(messageId: string): void {
    // Re-clicking the open message is a no-op: the router ignores a navigation
    // to the url it is already on, so nothing would reload -- but the loading
    // state below would still dim the whole list for half a second and make it
    // look like it did.
    if (messageId === this.selectedMessageId) {
      return;
    }

    this.loggingService.debug('Selecting message', { messageId });
    this.selectedMessageId = messageId;
    this.markRead(messageId);
    this.router.navigate(['message', messageId], {
      relativeTo: this.route,
      queryParamsHandling: 'preserve'
    });
  }



  /**
   * Opening a message clears its unread bold straight away, the way selecting a
   * row does on the desktop. The service records this too (it is what the next
   * list load reports), but waiting for that round trip would leave the row
   * bold under the reader's eyes.
   */
  private markRead(messageId: string): void {
    const message = this.allMessages.find(msg => msg.id === messageId);
    if (message && !message.isRead) {
      message.isRead = true;
    }
  }

  downloadSection(messageId: string, contentId: string): void {
    this.messageService.downloadSectionByContentId(messageId, contentId);
  }

  ngOnDestroy() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
      this.loadingTimeout = null;
    }
    
    // Stop SignalR connection
    this.signalRService.stop();
    
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