import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Observable, map, switchMap } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatTabsModule } from '@angular/material/tabs';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailListPipe } from '../../pipes/email-list.pipe';
import { CidTransformPipe } from '../../pipes/cid-transform.pipe';
import { MessageService } from '../../services/message.service';
import { DetailDto } from '../../models';

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
    MatTabsModule,
    FileSizePipe, 
    EmailListPipe, 
    CidTransformPipe
  ],
  template: `
    <div class="message-detail-container flex flex-col h-full bg-gray-50 dark:bg-gray-900 transition-colors duration-300">
      <!-- Header Section -->
      <div class="header-section flex-shrink-0 bg-white dark:bg-gray-800 shadow-md border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
        <div class="header-content flex items-center justify-between p-4 lg:p-6">
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

      <!-- Message Details Section -->
      <div class="message-details-section flex-shrink-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 transition-colors duration-300" *ngIf="message$ | async as message">
        <div class="message-details-content p-4 lg:p-6">
          <div class="details-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            
            <!-- From Section -->
            <div class="detail-item flex items-start gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
              <div class="detail-icon flex items-center justify-center w-8 h-8 bg-blue-100 dark:bg-blue-900 rounded-full flex-shrink-0">
                <mat-icon class="text-blue-600 dark:text-blue-400 text-sm">person</mat-icon>
              </div>
              <div class="detail-content flex-1 min-w-0">
                <h4 class="detail-label font-semibold text-gray-800 dark:text-white text-sm mb-1">From</h4>
                <p class="detail-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.from | emailList }}</p>
              </div>
            </div>
            
            <!-- To Section -->
            <div class="detail-item flex items-start gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
              <div class="detail-icon flex items-center justify-center w-8 h-8 bg-green-100 dark:bg-green-900 rounded-full flex-shrink-0">
                <mat-icon class="text-green-600 dark:text-green-400 text-sm">people</mat-icon>
              </div>
              <div class="detail-content flex-1 min-w-0">
                <h4 class="detail-label font-semibold text-gray-800 dark:text-white text-sm mb-1">To</h4>
                <p class="detail-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.to | emailList }}</p>
              </div>
            </div>
            
            <!-- CC Section -->
            <div *ngIf="message.cc?.length" class="detail-item flex items-start gap-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
              <div class="detail-icon flex items-center justify-center w-8 h-8 bg-yellow-100 dark:bg-yellow-900 rounded-full flex-shrink-0">
                <mat-icon class="text-yellow-600 dark:text-yellow-400 text-sm">people_outline</mat-icon>
              </div>
              <div class="detail-content flex-1 min-w-0">
                <h4 class="detail-label font-semibold text-gray-800 dark:text-white text-sm mb-1">CC</h4>
                <p class="detail-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.cc | emailList }}</p>
              </div>
            </div>
            
            <!-- BCC Section -->
            <div *ngIf="message.bCc?.length" class="detail-item flex items-start gap-3 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
              <div class="detail-icon flex items-center justify-center w-8 h-8 bg-red-100 dark:bg-red-900 rounded-full flex-shrink-0">
                <mat-icon class="text-red-600 dark:text-red-400 text-sm">visibility_off</mat-icon>
              </div>
              <div class="detail-content flex-1 min-w-0">
                <h4 class="detail-label font-semibold text-gray-800 dark:text-white text-sm mb-1">BCC</h4>
                <p class="detail-value text-gray-700 dark:text-gray-300 text-sm break-words">{{ message.bCc | emailList }}</p>
              </div>
            </div>
            
            <!-- Attachments Summary -->
            <div *ngIf="message.sections?.length" class="detail-item flex items-start gap-3 p-3 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
              <div class="detail-icon flex items-center justify-center w-8 h-8 bg-purple-100 dark:bg-purple-900 rounded-full flex-shrink-0">
                <mat-icon class="text-purple-600 dark:text-purple-400 text-sm">attach_file</mat-icon>
              </div>
              <div class="detail-content flex-1 min-w-0">
                <h4 class="detail-label font-semibold text-gray-800 dark:text-white text-sm mb-1">Attachments</h4>
                <p class="detail-value text-gray-700 dark:text-gray-300 text-sm">{{ message.sections.length }} attachment(s)</p>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Content Section with Tabs -->
      <div class="content-section flex-1 overflow-hidden" *ngIf="message$ | async as message">
        <div class="message-tabs h-full">
          <mat-tab-group class="h-full" dynamicHeight="false">
            
            <!-- Body Tab -->
            <mat-tab label="Body">
              <div class="tab-content h-full overflow-auto">
                <div class="body-content h-full p-4">
                  <div class="message-body leading-relaxed min-h-32 p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700" 
                       [innerHTML]="(message.htmlBody || message.textBody) | cidTransform:(message.id || '')">
                  </div>
                </div>
              </div>
            </mat-tab>
            
            <!-- Headers Tab -->
            <mat-tab label="Headers">
              <div class="tab-content h-full overflow-auto">
                <div class="headers-content p-4">
                  <div *ngFor="let header of message.headers" class="header-item">
                    <span class="header-name">{{ header.name }}:</span>
                    <span class="header-value">{{ header.value }}</span>
                  </div>
                </div>
              </div>
            </mat-tab>
            
            <!-- Sections Tab -->
            <mat-tab label="Sections" [disabled]="!message.sections?.length">
              <div class="tab-content h-full overflow-auto">
                <div class="sections-content p-4">
                  <div *ngFor="let section of message.sections" class="section-item">
                    <div class="section-header">
                      <mat-icon>{{ getSectionIcon(section.mediaType) }}</mat-icon>
                      <div class="flex-1">
                        <div class="section-type">{{ section.fileName || section.mediaType }}</div>
                        <div class="section-info">{{ section.mediaType }}</div>
                      </div>
                      <button 
                        mat-icon-button 
                        color="primary"
                        (click)="downloadSectionSafe(message, section.id!)"
                        *ngIf="section.id"
                        title="Download attachment">
                        <mat-icon>download</mat-icon>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </mat-tab>
            
          </mat-tab-group>
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

    // Headers styling
    .headers-content {
      @apply space-y-2;
    }

    .header-item {
      @apply flex flex-col sm:flex-row sm:items-center p-3 bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600;
    }

    .header-name {
      @apply font-semibold text-gray-800 dark:text-white text-sm mr-2 min-w-0;
    }

    .header-value {
      @apply text-gray-700 dark:text-gray-300 text-sm break-all;
    }

    // Sections styling
    .sections-content {
      @apply space-y-4;
    }

    .section-item {
      @apply bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600 p-4;
    }

    .section-header {
      @apply flex items-center gap-3;
    }

    .section-type {
      @apply font-semibold text-gray-800 dark:text-white text-sm;
    }

    .section-info {
      @apply text-gray-600 dark:text-gray-400 text-xs;
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
        color: #ffffff;
      }

      .message-body div {
        color: #ffffff;
      }
    }

    // Responsive design
    @media (max-width: 1024px) {
      .content-grid {
        flex-direction: column;
      }
      
      .sidebar-column {
        width: 100%;
        border-left: none;
        border-top: 1px solid;
        @apply border-gray-200 dark:border-gray-700;
      }
    }
  `]
})
export class MessageDetailComponent {
  message$: Observable<DetailDto>;
  private currentMessage: DetailDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private messageService: MessageService
  ) {
    this.message$ = this.route.params.pipe(
      switchMap(params => {
        const messageId = params['id'];
        if (messageId) {
          return this.messageService.getMessage(messageId);
        }
        throw new Error('Message ID not found');
      }),
      map(messageDetail => {
        this.currentMessage = messageDetail;
        return messageDetail;
      })
    );
  }

  downloadRaw() {
    if (this.currentMessage && this.currentMessage.id) {
      this.messageService.downloadRawMessage(this.currentMessage.id);
    }
  }

  downloadSection(messageId: string, contentId: string) {
    this.messageService.downloadSectionByContentId(messageId, contentId);
  }

  downloadSectionSafe(message: DetailDto, contentId: string) {
    if (message.id) {
      this.downloadSection(message.id, contentId);
    }
  }

  getSectionIcon(mediaType: string | null | undefined): string {
    if (!mediaType) {
      return 'attach_file';
    }

    if (mediaType.startsWith('image/')) {
      return 'image';
    } else if (mediaType.startsWith('text/')) {
      return 'description';
    } else if (mediaType.includes('pdf')) {
      return 'picture_as_pdf';
    } else if (mediaType.includes('word') || mediaType.includes('document')) {
      return 'article';
    } else if (mediaType.includes('spreadsheet') || mediaType.includes('excel')) {
      return 'table_chart';
    } else if (mediaType.includes('zip') || mediaType.includes('archive')) {
      return 'archive';
    } else {
      return 'attach_file';
    }
  }
}