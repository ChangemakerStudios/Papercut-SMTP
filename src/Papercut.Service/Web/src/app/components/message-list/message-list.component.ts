import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

interface Message {
  id: string;
  from: string;
  to: string;
  subject: string;
  receivedDate: string;
}

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="message-list">
      <div class="header">
        <h1>Messages</h1>
      </div>
      <div class="messages">
        <div *ngFor="let message of messages$ | async" class="message-item" [routerLink]="['/message', message.id]">
          <div class="message-header">
            <span class="from">{{ message.from }}</span>
            <span class="date">{{ message.receivedDate | date:'short' }}</span>
          </div>
          <div class="subject">{{ message.subject }}</div>
          <div class="to">To: {{ message.to }}</div>
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

    .from {
      font-weight: 500;
    }

    .date {
      color: #666;
      font-size: 0.9rem;
    }

    .subject {
      font-size: 1.1rem;
      margin-bottom: 0.5rem;
    }

    .to {
      color: #666;
      font-size: 0.9rem;
    }
  `]
})
export class MessageListComponent implements OnInit {
  messages$: Observable<Message[]>;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.messages$ = this.http.get<Message[]>('/api/messages');
  }
} 