import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap } from 'rxjs';

interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  size: number;
}

interface MessageDetail {
  id: string;
  from: string;
  to: string;
  subject: string;
  receivedDate: string;
  body: string;
  attachments: Attachment[];
}

@Component({
  selector: 'app-message-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="message-detail">
      <div class="header">
        <button class="back-button" routerLink="/">← Back</button>
        <h1>{{ (message$ | async)?.subject }}</h1>
      </div>
      <div class="content" *ngIf="message$ | async as message">
        <div class="message-header">
          <div class="from">From: {{ message.from }}</div>
          <div class="to">To: {{ message.to }}</div>
          <div class="date">Received: {{ message.receivedDate | date:'medium' }}</div>
        </div>
        <div class="body" [innerHTML]="message.body"></div>
        <div class="attachments" *ngIf="message.attachments?.length">
          <h3>Attachments</h3>
          <div class="attachment-list">
            <div *ngFor="let attachment of message.attachments" class="attachment-item">
              <span class="filename">{{ attachment.fileName }}</span>
              <span class="size">({{ attachment.size | fileSize }})</span>
              <button (click)="downloadAttachment(attachment)">Download</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .message-detail {
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
      align-items: center;
      gap: 1rem;
    }

    .back-button {
      padding: 0.5rem 1rem;
      border: none;
      background: none;
      cursor: pointer;
      font-size: 1rem;
      color: #666;

      &:hover {
        color: #000;
      }
    }

    .content {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;
    }

    .message-header {
      background-color: #fff;
      padding: 1rem;
      border-radius: 4px;
      margin-bottom: 1rem;
    }

    .from, .to, .date {
      margin-bottom: 0.5rem;
    }

    .body {
      background-color: #fff;
      padding: 1rem;
      border-radius: 4px;
      margin-bottom: 1rem;
    }

    .attachments {
      background-color: #fff;
      padding: 1rem;
      border-radius: 4px;
    }

    .attachment-list {
      margin-top: 0.5rem;
    }

    .attachment-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.5rem;
      border-bottom: 1px solid #e0e0e0;

      &:last-child {
        border-bottom: none;
      }
    }

    .filename {
      font-weight: 500;
    }

    .size {
      color: #666;
    }

    button {
      padding: 0.25rem 0.5rem;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      background: none;
      cursor: pointer;

      &:hover {
        background-color: #f5f6f8;
      }
    }
  `]
})
export class MessageDetailComponent implements OnInit {
  message$: Observable<MessageDetail>;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.message$ = this.route.params.pipe(
      switchMap(params => this.http.get<MessageDetail>(`/api/messages/${params['id']}`))
    );
  }

  downloadAttachment(attachment: Attachment) {
    window.open(`/api/messages/${attachment.id}/download`, '_blank');
  }
} 