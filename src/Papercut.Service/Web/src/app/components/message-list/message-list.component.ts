import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Observable, map } from 'rxjs';

interface Message {
  id: string;
  subject: string;
  size: string;
  createdAt: string;
}

interface MessageResponse {
  totalMessageCount: number;
  messages: Message[];
}

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="message-list">
      <div class="header">
        <h1>Messages ({{ (messages$ | async)?.totalMessageCount }})</h1>
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
  `]
})
export class MessageListComponent {
  messages$: Observable<MessageResponse>;

  constructor(private route: ActivatedRoute) {
    this.messages$ = this.route.data.pipe(
      map(data => data['messages'])
    );
  }
} 