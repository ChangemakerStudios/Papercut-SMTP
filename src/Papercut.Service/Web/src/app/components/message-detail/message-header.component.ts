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
import { EmailAddressDisplayComponent, EmailAddress } from '../shared/email-address-display.component';
import { AttachmentSummaryComponent, Attachment } from '../shared/attachment-summary.component';
import { DetailDto, RefDto } from '../../models';

/**
 * Component responsible for displaying the message header section.
 * Extracted from MessageDetailComponent to follow Single Responsibility Principle.
 * This component handles the display of message metadata including sender, recipients, and attachments.
 */
@Component({
  selector: 'app-message-header',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    EmailAddressDisplayComponent,
    AttachmentSummaryComponent
  ],
  template: `
    <!-- Message Header Section -->
    <div class="flex-shrink-0 bg-white dark:bg-gray-800 shadow-md border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
      <div class="header-content flex items-center justify-between p-3 lg:p-4">
        <!-- Subject Section -->
        <div class="subject-section flex-1 min-w-0">
          <h1 class="message-title text-xl lg:text-2xl font-semibold text-gray-800 dark:text-white truncate m-0">
            {{ (message?.detail?.subject || message?.ref?.subject) || '(No Subject)' }}
          </h1>
          <p class="message-date text-sm text-gray-600 dark:text-gray-400 mt-1">
            {{ (message?.detail?.createdAt || message?.ref?.createdAt) | date:'full' }}
          </p>
        </div>
      </div>
    </div>

    <!-- Message Details Section -->
    <div class="flex-shrink-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 transition-colors duration-300">
      <div class="message-details-content p-3 lg:p-4">
        <div class="details-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          
          <!-- From Section -->
          <app-email-address-display
            [emailAddresses]="getFromAddresses()"
            label="From"
            type="from">
          </app-email-address-display>
          
          <!-- To Section -->
          <app-email-address-display
            *ngIf="getToAddresses().length"
            [emailAddresses]="getToAddresses()"
            label="To"
            type="to">
          </app-email-address-display>
          
          <!-- CC Section -->
          <app-email-address-display
            *ngIf="getCcAddresses().length"
            [emailAddresses]="getCcAddresses()"
            label="CC"
            type="cc">
          </app-email-address-display>
          
          <!-- BCC Section -->
          <app-email-address-display
            *ngIf="getBccAddresses().length"
            [emailAddresses]="getBccAddresses()"
            label="BCC"
            type="bcc">
          </app-email-address-display>
          
          <!-- Attachments Summary -->
          <app-attachment-summary
            *ngIf="getAttachments().length || getAttachmentCount() > 0"
            [attachments]="getAttachments()"
            [showPreview]="true"
            [maxPreviewItems]="2"
            (viewAttachments)="onViewAttachments()">
          </app-attachment-summary>
        </div>
      </div>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageHeaderComponent {
  @Input() message: { detail: DetailDto | null; ref: RefDto | null } | null = null;

  @Output() viewAttachments = new EventEmitter<void>();
  @Output() emailClick = new EventEmitter<EmailAddress>();

  getFromAddresses(): EmailAddress[] {
    if (this.message?.detail?.from) {
      return this.message.detail.from.map(addr => ({
        name: addr.name || undefined,
        address: addr.address || ''
      }));
    } else if (this.message?.ref?.from) {
      return this.message.ref.from.map(addr => ({
        name: addr.name || undefined,
        address: addr.address || ''
      }));
    }
    return [];
  }

  getToAddresses(): EmailAddress[] {
    if (this.message?.detail?.to) {
      return this.message.detail.to.map(addr => ({
        name: addr.name || undefined,
        address: addr.address || ''
      }));
    }
    return [];
  }

  getCcAddresses(): EmailAddress[] {
    if (this.message?.detail?.cc) {
      return this.message.detail.cc.map(addr => ({
        name: addr.name || undefined,
        address: addr.address || ''
      }));
    }
    return [];
  }

  getBccAddresses(): EmailAddress[] {
    if (this.message?.detail?.bcc) {
      return this.message.detail.bcc.map(addr => ({
        name: addr.name || undefined,
        address: addr.address || ''
      }));
    }
    return [];
  }

  getAttachments(): Attachment[] {
    if (this.message?.detail?.attachments) {
      return this.message.detail.attachments.map(att => ({
        id: att.id || undefined,
        fileName: att.fileName || undefined,
        mediaType: att.mediaType || undefined,
        size: undefined // EmailSectionDto doesn't have size property
      }));
    }
    return [];
  }

  getAttachmentCount(): number {
    if (this.message?.detail?.attachments) {
      return this.message.detail.attachments.length;
    } else if (this.message?.ref?.attachmentCount) {
      return this.message.ref.attachmentCount;
    }
    return 0;
  }

  onViewAttachments(): void {
    this.viewAttachments.emit();
  }

  onEmailClick(email: EmailAddress): void {
    this.emailClick.emit(email);
  }
}
