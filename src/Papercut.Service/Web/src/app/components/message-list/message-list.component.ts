import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Observable, map, combineLatest } from 'rxjs';
import { Message, MessageResponse } from '../../services/message.repository';

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
  imports: [CommonModule, RouterModule],
  template: `
    <div class="message-list">
      <div class="header">
        <h1>Messages ({{ (pagination$ | async)?.totalCount }})</h1>
        <div class="pagination-info" *ngIf="pagination$ | async as pagination">
          Page {{ pagination.currentPage }} of {{ pagination.totalPages }}
        </div>
      </div>
      <div class="messages">
        <div *ngFor="let message of (messages$ | async)?.messages" class="message-item" [routerLink]="['/message', message.id]">
          <div class="message-header">
            <span class="subject">{{ message.subject }}</span>
            <span class="date">{{ message.createdAt | date:'short' }}</span>
          </div>
          <div class="size">{{ message.size }}</div>
        </div>
      </div>
      <div class="pagination" *ngIf="pagination$ | async as pagination">
        <button 
          [disabled]="!pagination.hasPrevious" 
          (click)="goToPage(pagination.currentPage - 1)"
          class="pagination-btn">
          ← Previous
        </button>
        <span class="page-info">
          {{ pagination.start + 1 }}-{{ Math.min(pagination.start + pagination.limit, pagination.totalCount) }} 
          of {{ pagination.totalCount }}
        </span>
        <button 
          [disabled]="!pagination.hasNext" 
          (click)="goToPage(pagination.currentPage + 1)"
          class="pagination-btn">
          Next →
        </button>
      </div>
    </div>
  `,
  styles: [`
    .message-list {
      height: 100%;
      display: flex;
      flex-direction: column;
      background-color: #f5f6f8;
    }

    .header {
      padding: 1rem;
      background-color: #fff;
      border-bottom: 1px solid #e0e0e0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .pagination-info {
      color: #666;
      font-size: 0.9rem;
    }

    .messages {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;
    }

    .message-item {
      background-color: #fff;
      border-radius: 4px;
      padding: 1rem;
      margin-bottom: 1rem;
      cursor: pointer;
      transition: box-shadow 0.2s;

      &:hover {
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      }
    }

    .message-header {
      display: flex;
      justify-content: space-between;
      margin-bottom: 0.5rem;
    }

    .subject {
      font-weight: 500;
      flex: 1;
      margin-right: 1rem;
    }

    .date {
      color: #666;
      font-size: 0.9rem;
      white-space: nowrap;
    }

    .size {
      color: #666;
      font-size: 0.9rem;
    }

    .pagination {
      padding: 1rem;
      background-color: #fff;
      border-top: 1px solid #e0e0e0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .pagination-btn {
      padding: 0.5rem 1rem;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      background: #fff;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover:not(:disabled) {
        background-color: #f5f6f8;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .page-info {
      color: #666;
      font-size: 0.9rem;
    }
  `]
})
export class MessageListComponent {
  messages$: Observable<MessageResponse>;
  pagination$: Observable<PaginationInfo>;
  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router
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