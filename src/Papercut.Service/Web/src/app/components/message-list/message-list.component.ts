import { Component, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Observable, map, combineLatest, BehaviorSubject, switchMap, of, finalize } from 'rxjs';
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
    CidTransformPipe
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

      <!-- Message Detail Panel -->
      <div class="message-detail-panel">
        <div *ngIf="selectedMessage$ | async as message; else noMessageSelected" class="message-details">
          <div class="message-details-header">
            <div class="message-subject">{{ message.subject || '(No Subject)' }}</div>
            <div class="message-from">
              <mat-icon>person</mat-icon>
              {{ message.from | emailList }}
            </div>
            <div class="message-meta">
              <div class="message-meta-item">
                <mat-icon>schedule</mat-icon>
                {{ message.createdAt | date:'medium' }}
              </div>
              <div class="message-meta-item" *ngIf="message.to?.length">
                <mat-icon>people</mat-icon>
                {{ message.to | emailList }}
              </div>
            </div>
          </div>
          
          <div class="message-content">
            <div class="message-body" 
                 [innerHTML]="(message.htmlBody || message.textBody) | cidTransform:(message.id || '')">
            </div>
            
            <div *ngIf="message.sections?.length" class="attachments">
              <h4>Attachments ({{ message.sections.length }})</h4>
              <div *ngFor="let section of message.sections" class="attachment-item">
                <mat-icon>attach_file</mat-icon>
                <span>{{ section.fileName || section.mediaType }}</span>
                <button mat-button (click)="downloadSection(message.id!, section.id!)">
                  <mat-icon>download</mat-icon>
                  Download
                </button>
              </div>
            </div>
          </div>
        </div>
        
        <ng-template #noMessageSelected>
          <div class="no-message">
            <mat-icon>email</mat-icon>
            <h3>No message selected</h3>
            <p>Select a message from the list to view its contents</p>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [``]
})
export class MessageListComponent implements OnDestroy {
  messages$: Observable<GetMessagesResponse> = this.route.data.pipe(
    map(data => data['messages'])
  );
  pagination$: Observable<PaginationInfo>;
  selectedMessage$: Observable<DetailDto | null>;
  
  private selectedMessageId$ = new BehaviorSubject<string | null>(null);
  selectedMessageId: string | null = null;
  isLoadingMessage = false;
  private loadingTimeout: any = null;
  
  // Virtual scroll settings  
  itemSize = 80; // Height of each message item in pixels
  allMessages: RefDto[] = [];
  private currentPage = 1;
  isLoadingMore = false;
  private hasMorePages = true;
  private readonly pageSize = 10;

  @ViewChild(CdkVirtualScrollViewport) viewport!: CdkVirtualScrollViewport;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) {
    this.messages$.subscribe((response: GetMessagesResponse) => {
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

  trackByMessageId(index: number, message: RefDto): string {
    return message.id || index.toString();
  }

  selectMessage(messageId: string): void {
    console.log('Selecting message:', messageId);
    this.selectedMessageId = messageId;
    this.selectedMessageId$.next(messageId);
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
  }
} 