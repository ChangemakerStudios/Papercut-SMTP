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
import { LucideAngularModule, Paperclip } from 'lucide-angular';
import { EmailAddress } from '../shared/email-address-display.component';
import { Attachment } from '../shared/attachment-summary.component';
import { DetailDto, RefDto } from '../../models';

/**
 * Message header: the desktop app's labeled field rows (From / To / Date /
 * Priority / Subject), refined. Addresses render in mono; priority gets
 * semantic color; attachments are summarized inline.
 */
@Component({
  selector: 'app-message-header',
  imports: [
    CommonModule,
    LucideAngularModule
  ],
  template: `
    <div class="header-fields flex-shrink-0">
      <div class="field-row">
        <span class="field-label">From</span>
        <span class="field-value pc-mono">{{ formatAddresses(getFromAddresses()) || '—' }}</span>
      </div>
      <div class="field-row" *ngIf="getToAddresses().length">
        <span class="field-label">To</span>
        <span class="field-value pc-mono">{{ formatAddresses(getToAddresses()) }}</span>
      </div>
      <div class="field-row" *ngIf="getCcAddresses().length">
        <span class="field-label">CC</span>
        <span class="field-value pc-mono">{{ formatAddresses(getCcAddresses()) }}</span>
      </div>
      <div class="field-row" *ngIf="getBccAddresses().length">
        <span class="field-label">BCC</span>
        <span class="field-value pc-mono">{{ formatAddresses(getBccAddresses()) }}</span>
      </div>
      <div class="field-row">
        <span class="field-label">Date</span>
        <span class="field-value pc-mono">{{ (message?.detail?.createdAt || message?.ref?.createdAt) | date:'M/d/yyyy h:mm:ss a ZZZZZ' }}</span>
      </div>
      <div class="field-row" *ngIf="getPriority() as priority">
        <span class="field-label">Priority</span>
        <span class="field-value field-priority" [class.priority-urgent]="priority === 'Urgent'">{{ priority }}</span>
      </div>
      <div class="field-row">
        <span class="field-label">Subject</span>
        <span class="field-value field-subject">{{ (message?.detail?.subject || message?.ref?.subject) || '(No Subject)' }}</span>
      </div>
    </div>
  `,
  styles: [`
    .header-fields {
      background: var(--pc-surface);
      border-bottom: 1px solid var(--pc-border);
      padding: 10px 16px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .field-row {
      display: flex;
      align-items: baseline;
      gap: 12px;
      min-width: 0;
    }

    .field-label {
      flex-shrink: 0;
      width: 52px;
      text-align: right;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--pc-faint);
      user-select: none;
    }

    .field-value {
      flex: 1;
      min-width: 0;
      font-size: 12.5px;
      color: var(--pc-ink);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .field-subject {
      font-weight: 700;
      font-size: 13.5px;
      color: var(--pc-ink-strong);
      white-space: normal;
      line-height: 1.35;
    }

    .field-priority {
      font-weight: 600;
    }

    .priority-urgent {
      color: var(--pc-danger);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageHeaderComponent {
  protected readonly icons = { Paperclip };

  @Input() message: { detail: DetailDto | null; ref: RefDto | null } | null = null;

  @Output() viewAttachments = new EventEmitter<void>();
  @Output() emailClick = new EventEmitter<EmailAddress>();

  formatAddresses(addresses: EmailAddress[]): string {
    return addresses
      .map(a => a.name ? `${a.name} <${a.address}>` : a.address)
      .join(', ');
  }

  getPriority(): string | null {
    const priority = this.message?.ref?.priority;
    return priority && priority !== 'Normal' ? priority : null;
  }

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
        size: att.size ?? undefined
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

  getAttachmentSummary(): string {
    const count = this.getAttachmentCount();
    const names = this.getAttachments()
      .map(a => a.fileName)
      .filter(Boolean);

    if (names.length === 1) {
      return names[0]!;
    }

    return count === 1 ? '1 attachment' : `${count} attachments`;
  }

  onViewAttachments(): void {
    this.viewAttachments.emit();
  }

  onEmailClick(email: EmailAddress): void {
    this.emailClick.emit(email);
  }
}
