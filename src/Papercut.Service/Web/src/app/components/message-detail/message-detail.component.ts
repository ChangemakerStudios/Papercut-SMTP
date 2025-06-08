import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Observable, map } from 'rxjs';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { MessageRepository, MessageDetail, EmailAddress, Section } from '../../services/message.repository';

@Component({
  selector: 'app-message-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FileSizePipe, EmailListPipe, CidTransformPipe],
  template: `
    <div class="message-detail">
      <div class="header">
        <button class="back-button" routerLink="/">← Back</button>
        <h1>{{ (message$ | async)?.subject }}</h1>
        <button class="download-raw-btn" (click)="downloadRaw()" *ngIf="message$ | async as message">
          Download Raw
        </button>
      </div>
      <div class="content" *ngIf="message$ | async as message">
        <div class="message-header">
          <div class="from">From: {{ message.from | emailList }}</div>
          <div class="to">To: {{ message.to | emailList }}</div>
          <div class="cc" *ngIf="message.cc?.length">CC: {{ message.cc | emailList }}</div>
          <div class="bcc" *ngIf="message.bCc?.length">BCC: {{ message.bCc | emailList }}</div>
          <div class="date">Received: {{ message.createdAt | date:'medium' }}</div>
        </div>
        <div class="body" [innerHTML]="(message.htmlBody || message.textBody) | cidTransform:message.id"></div>
        <div class="sections" *ngIf="message.sections?.length">
          <h3>Attachments</h3>
          <div class="section-list">
            <ng-container *ngFor="let section of message.sections">
              <div *ngIf="section.id" class="section-item">
                <span class="filename">{{ section.fileName || section.mediaType }}</span>
                <button (click)="downloadSection(message.id, section.id!)">Download</button>
              </div>
            </ng-container>
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

    .back-button, .download-raw-btn {
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

    .download-raw-btn {
      margin-left: auto;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      background: #fff;

      &:hover {
        background-color: #f5f6f8;
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

    .from, .to, .cc, .bcc, .date {
      margin-bottom: 0.5rem;
    }

    .body {
      background-color: #fff;
      padding: 1rem;
      border-radius: 4px;
      margin-bottom: 1rem;
    }

    .sections {
      background-color: #fff;
      padding: 1rem;
      border-radius: 4px;
    }

    .section-list {
      margin-top: 0.5rem;
    }

    .section-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.5rem;
      border-bottom: 1px solid #e0e0e0;

      &:last-child {
        border-bottom: none;
      }
    }

    .filename {
      font-weight: 500;
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
export class MessageDetailComponent {
  message$: Observable<MessageDetail>;
  private currentMessage: MessageDetail | null = null;

  constructor(
    private route: ActivatedRoute,
    private messageRepository: MessageRepository
  ) {
    this.message$ = this.route.data.pipe(
      map(data => data['message'])
    );

    // Keep track of current message for download operations
    this.message$.subscribe(message => {
      this.currentMessage = message;
    });
  }

  downloadRaw() {
    if (this.currentMessage) {
      this.messageRepository.downloadRawMessage(this.currentMessage.id);
    }
  }

  downloadSection(messageId: string, contentId: string) {
    this.messageRepository.downloadSectionByContentId(messageId, contentId);
  }
}