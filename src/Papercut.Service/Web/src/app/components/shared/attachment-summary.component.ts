// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

export interface Attachment {
  id?: string;
  fileName?: string;
  mediaType?: string;
  size?: number;
}

/**
 * Component for displaying attachment summaries in a consistent, styled format.
 * Shows attachment count and basic information with optional actions.
 * This component focuses solely on attachment summary presentation.
 */
@Component({
  selector: 'app-attachment-summary',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="flex items-start gap-2 p-2 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
      <!-- Icon -->
      <div class="flex items-center justify-center w-8 h-8 bg-purple-100 dark:bg-purple-800/40 rounded-full flex-shrink-0">
        <mat-icon class="text-purple-600 dark:text-purple-400 flex items-center justify-center">attach_file</mat-icon>
      </div>
      
      <!-- Content -->
      <div class="flex-1 min-w-0">
        <h4 class="font-semibold text-gray-800 dark:text-gray-100 text-sm mb-0.5">Attachments</h4>
        <div class="text-gray-700 dark:text-gray-300 text-sm">
          <ng-container *ngIf="attachments && attachments.length > 0; else noAttachments">
            <div class="flex items-center justify-between">
              <span>{{ attachments.length }} attachment(s)</span>
              <button 
                *ngIf="showViewButton"
                mat-icon-button 
                color="primary" 
                size="small"
                (click)="onViewAttachments()"
                title="View attachments">
                <mat-icon>visibility</mat-icon>
              </button>
            </div>
            
            <!-- Attachment preview (optional) -->
            <div *ngIf="showPreview && attachments.length > 0" class="mt-2 space-y-1">
              <div *ngFor="let attachment of attachments.slice(0, maxPreviewItems)" 
                   class="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
                <mat-icon class="text-xs">{{ getAttachmentIcon(attachment) }}</mat-icon>
                <span class="truncate">{{ attachment.fileName || 'Unnamed attachment' }}</span>
                <span *ngIf="attachment.size" class="text-gray-500">
                  ({{ formatFileSize(attachment.size) }})
                </span>
              </div>
              <div *ngIf="attachments.length > maxPreviewItems" class="text-xs text-gray-500 italic">
                +{{ attachments.length - maxPreviewItems }} more
              </div>
            </div>
          </ng-container>
          
          <ng-template #noAttachments>
            <span class="text-gray-500 dark:text-gray-400 italic">No attachments</span>
          </ng-template>
        </div>
      </div>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AttachmentSummaryComponent {
  @Input() attachments: Attachment[] = [];
  @Input() showPreview: boolean = false;
  @Input() maxPreviewItems: number = 3;
  @Input() showViewButton: boolean = true;

  @Output() viewAttachments = new EventEmitter<void>();
  @Output() attachmentClick = new EventEmitter<Attachment>();

  onViewAttachments(): void {
    this.viewAttachments.emit();
  }

  onAttachmentClick(attachment: Attachment): void {
    this.attachmentClick.emit(attachment);
  }

  getAttachmentIcon(attachment: Attachment): string {
    const fileName = attachment.fileName || '';
    const mediaType = attachment.mediaType || '';
    
    if (fileName.includes('.jpg') || fileName.includes('.jpeg') || fileName.includes('.png') || 
        fileName.includes('.gif') || mediaType.includes('image')) {
      return 'image';
    } else if (fileName.includes('.pdf') || mediaType.includes('pdf')) {
      return 'picture_as_pdf';
    } else if (fileName.includes('.doc') || fileName.includes('.docx') || 
               mediaType.includes('word') || mediaType.includes('document')) {
      return 'article';
    } else if (fileName.includes('.xls') || fileName.includes('.xlsx') || 
               mediaType.includes('spreadsheet') || mediaType.includes('excel')) {
      return 'table_chart';
    } else if (fileName.includes('.zip') || fileName.includes('.rar') || 
               mediaType.includes('archive') || mediaType.includes('zip')) {
      return 'archive';
    } else if (fileName.includes('.txt') || mediaType.includes('text')) {
      return 'description';
    } else {
      return 'attach_file';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    const value = bytes / Math.pow(k, i);
    // Only show decimal for values less than 10
    const formattedValue = value < 10 ? value.toFixed(1) : Math.round(value).toString();
    
    return formattedValue + ' ' + sizes[i];
  }
}
