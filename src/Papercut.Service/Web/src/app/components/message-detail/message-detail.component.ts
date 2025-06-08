import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Observable, map } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { MessageRepository, MessageDetail, EmailAddress, Section } from '../../services/message.repository';

@Component({
  selector: 'app-message-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatListModule,
    FileSizePipe, 
    EmailListPipe, 
    CidTransformPipe
  ],
  template: `
    <div class="message-detail-container flex flex-col h-full bg-gray-50 dark:bg-gray-900 transition-colors duration-300">
      <!-- Header Section -->
      <div class="header-section flex-shrink-0 bg-white dark:bg-gray-800 shadow-md border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
        <div class="header-content flex items-center gap-4 p-4 lg:p-6">
          <!-- Back Button -->
          <button mat-icon-button 
                  routerLink="/" 
                  class="back-button flex-shrink-0 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors duration-200">
            <mat-icon class="text-gray-600 dark:text-gray-400">arrow_back</mat-icon>
          </button>
          
          <!-- Subject Section -->
          <div class="subject-section flex-1 min-w-0">
            <h1 class="message-title text-xl lg:text-2xl font-semibold text-gray-800 dark:text-white truncate m-0">
              {{ (message$ | async)?.subject || '(No Subject)' }}
            </h1>
            <p class="message-date text-sm text-gray-600 dark:text-gray-400 mt-1" *ngIf="message$ | async as message">
              {{ message.createdAt | date:'full' }}
            </p>
          </div>
          
          <!-- Action Buttons -->
          <div class="action-section flex-shrink-0 flex items-center gap-2">
            <button 
              mat-raised-button 
              color="accent" 
              (click)="downloadRaw()" 
              *ngIf="message$ | async as message"
              class="download-btn flex items-center gap-2 shadow-lg hover:shadow-xl transition-all duration-200">
              <mat-icon>download</mat-icon>
              <span class="hidden sm:inline">Download Raw</span>
            </button>
          </div>
        </div>
      </div>
      
      <!-- Content Section -->
      <div class="content-section flex-1 overflow-y-auto" *ngIf="message$ | async as message">
        <div class="content-grid flex flex-col lg:flex-row gap-6 p-4 lg:p-6 max-w-7xl mx-auto">
          
          <!-- Main Content Column -->
          <div class="main-column flex-1 flex flex-col gap-6">
            
            <!-- Message Content Card -->
            <mat-card class="message-body-card shadow-lg hover:shadow-xl transition-shadow duration-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
              <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-500 dark:from-purple-600 dark:to-pink-600 rounded-full">
                  <mat-icon class="text-white text-2xl">article</mat-icon>
                </div>
                <div class="header-text">
                  <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Message Content</h2>
                  <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">Email body and content</p>
                </div>
              </div>
              
              <div class="card-content p-6">
                <div class="message-body leading-relaxed min-h-32 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border-l-4 border-l-primary-400 dark:border-l-primary-500" 
                     [innerHTML]="(message.htmlBody || message.textBody) | cidTransform:message.id">
                </div>
              </div>
            </mat-card>

            <!-- Attachments Card -->
            <mat-card *ngIf="message.sections?.length" 
                      class="attachments-card shadow-lg hover:shadow-xl transition-shadow duration-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
              <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-orange-500 to-red-500 dark:from-orange-600 dark:to-red-600 rounded-full">
                  <mat-icon class="text-white text-2xl">attach_file</mat-icon>
                </div>
                <div class="header-text">
                  <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Attachments</h2>
                  <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">{{ message.sections.length }} attachment(s)</p>
                </div>
              </div>
              
              <div class="card-content p-6">
                <div class="attachments-grid flex flex-col gap-3">
                  <div *ngFor="let section of message.sections; let last = last" 
                       class="attachment-item">
                    <div *ngIf="section.id" 
                         class="attachment-content flex items-center gap-4 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors duration-200">
                      <div class="attachment-icon flex items-center justify-center w-10 h-10 bg-blue-100 dark:bg-blue-900 rounded-full flex-shrink-0">
                        <mat-icon class="text-blue-600 dark:text-blue-400">insert_drive_file</mat-icon>
                      </div>
                      
                      <div class="attachment-info flex-1 min-w-0">
                        <h4 class="attachment-name font-semibold text-gray-800 dark:text-white truncate">
                          {{ section.fileName || section.mediaType }}
                        </h4>
                        <p class="attachment-type text-gray-600 dark:text-gray-400 text-sm">{{ section.mediaType }}</p>
                      </div>
                      
                      <button 
                        mat-raised-button 
                        color="primary"
                        (click)="downloadSection(message.id, section.id!)"
                        class="download-attachment-btn flex items-center gap-2 shadow-md hover:shadow-lg transition-all duration-200 flex-shrink-0">
                        <mat-icon class="text-sm">download</mat-icon>
                        <span class="hidden sm:inline">Download</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </mat-card>
          </div>
          
          <!-- Sidebar Column -->
          <div class="sidebar-column flex-shrink-0 w-full lg:w-80">
            <mat-card class="message-info-card shadow-lg hover:shadow-xl transition-shadow duration-300 sticky top-6 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
              <div class="card-header flex items-center gap-4 p-6 border-b border-gray-100 dark:border-gray-700">
                <div class="header-icon flex items-center justify-center w-12 h-12 bg-gradient-to-br from-primary-500 to-accent-500 dark:from-primary-600 dark:to-accent-600 rounded-full">
                  <mat-icon class="text-white text-2xl">email</mat-icon>
                </div>
                <div class="header-text">
                  <h2 class="text-xl font-semibold text-gray-800 dark:text-white m-0">Message Details</h2>
                  <p class="text-gray-600 dark:text-gray-400 text-sm mt-1">Sender and recipient information</p>
                </div>
              </div>
              
              <div class="card-content p-6">
                <div class="info-grid flex flex-col gap-4">
                  
                  <!-- From Section -->
                  <div class="info-item flex items-start gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <div class="info-icon flex items-center justify-center w-8 h-8 bg-blue-100 dark:bg-blue-900 rounded-full flex-shrink-0">
                      <mat-icon class="text-blue-600 dark:text-blue-400 text-sm">person</mat-icon>
                    </div>
                    <div class="info-content flex-1 min-w-0">
                      <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">From</h4>
                      <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.from | emailList }}</p>
                    </div>
                  </div>
                  
                  <!-- To Section -->
                  <div class="info-item flex items-start gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <div class="info-icon flex items-center justify-center w-8 h-8 bg-green-100 dark:bg-green-900 rounded-full flex-shrink-0">
                      <mat-icon class="text-green-600 dark:text-green-400 text-sm">people</mat-icon>
                    </div>
                    <div class="info-content flex-1 min-w-0">
                      <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">To</h4>
                      <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.to | emailList }}</p>
                    </div>
                  </div>
                  
                  <!-- CC Section -->
                  <div *ngIf="message.cc?.length" class="info-item flex items-start gap-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                    <div class="info-icon flex items-center justify-center w-8 h-8 bg-yellow-100 dark:bg-yellow-900 rounded-full flex-shrink-0">
                      <mat-icon class="text-yellow-600 dark:text-yellow-400 text-sm">people_outline</mat-icon>
                    </div>
                    <div class="info-content flex-1 min-w-0">
                      <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">CC</h4>
                      <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.cc | emailList }}</p>
                    </div>
                  </div>
                  
                  <!-- BCC Section -->
                  <div *ngIf="message.bCc?.length" class="info-item flex items-start gap-3 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                    <div class="info-icon flex items-center justify-center w-8 h-8 bg-red-100 dark:bg-red-900 rounded-full flex-shrink-0">
                      <mat-icon class="text-red-600 dark:text-red-400 text-sm">visibility_off</mat-icon>
                    </div>
                    <div class="info-content flex-1 min-w-0">
                      <h4 class="info-label font-semibold text-gray-800 dark:text-white text-sm mb-1">BCC</h4>
                      <p class="info-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.bCc | emailList }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </mat-card>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .message-body {
      @apply min-h-32;
    }

    .message-body img {
      @apply max-w-full h-auto rounded-lg shadow-sm;
    }

    .message-body p {
      @apply mb-4 last:mb-0;
    }

    .message-body a {
      @apply text-primary-600 dark:text-primary-400 hover:text-primary-800 dark:hover:text-primary-300 underline;
    }

    .sidebar-column .message-info-card {
      @apply lg:sticky lg:top-6;
    }

    .attachment-content:hover {
      @apply bg-gray-100 dark:bg-gray-800 border-gray-300 dark:border-gray-600;
    }

    .info-item {
      @apply transition-all duration-200 hover:shadow-sm;
    }

    // Dark theme specific overrides
    :host-context(body[data-theme="dark"]) {
      .message-detail-container {
        background-color: #121212;
      }

      .message-body {
        color: #ffffff;
        background-color: #1f2937;
      }

      .message-body h1, 
      .message-body h2, 
      .message-body h3, 
      .message-body h4, 
      .message-body h5, 
      .message-body h6 {
        color: #ffffff;
      }

      .message-body p {
        color: #e5e7eb;
      }

      .content-section {
        scrollbar-width: thin;
        scrollbar-color: #374151 #1f2937;
      }

      .content-section::-webkit-scrollbar {
        width: 8px;
      }

      .content-section::-webkit-scrollbar-track {
        background-color: #1f2937;
      }

      .content-section::-webkit-scrollbar-thumb {
        background-color: #374151;
        border-radius: 4px;
      }

      .content-section::-webkit-scrollbar-thumb:hover {
        background-color: #4b5563;
      }
    }

    @media (max-width: 1024px) {
      .content-grid {
        @apply flex-col;
      }
      
      .sidebar-column {
        @apply w-full;
      }
      
      .sidebar-column .message-info-card {
        @apply relative top-auto;
      }
    }

    @media (max-width: 640px) {
      .header-content {
        @apply p-4;
      }
      
      .content-grid {
        @apply p-4;
      }
      
      .download-btn span,
      .download-attachment-btn span {
        @apply hidden;
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