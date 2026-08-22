import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, Paperclip, ChevronsUp, ChevronsDown } from 'lucide-angular';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailService } from '../../services/email.service';
import { RefDto } from 'src/app/models';

@Component({
  selector: 'app-message-list-item',
  imports: [CommonModule, MatTooltipModule, LucideAngularModule, FileSizePipe],
  template: `
    <div class="msg-item cursor-pointer"
         [ngClass]="{
           'msg-selected': selected,
           'msg-in-selection': inSelection && !selected,
           'msg-unread': !message.isRead
         }"
         (click)="onSelect($event)">
      <div class="msg-subject" [matTooltip]="message.subject ?? 'No Subject'" matTooltipShowDelay="700">
        {{ message.subject ?? '(No Subject)' }}
      </div>
      <div class="msg-meta">
        <span class="msg-from pc-mono" [matTooltip]="(message.createdAt | date:'full') ?? ''" matTooltipShowDelay="700">
          {{ getFromDisplay() }}
        </span>
        <span class="msg-indicators">
          <lucide-icon *ngIf="message.attachmentCount && message.attachmentCount > 0"
                       [img]="icons.Paperclip" [size]="12"
                       [matTooltip]="getAttachmentTooltip()"></lucide-icon>
          <lucide-icon *ngIf="message.priority === 'Urgent'"
                       [img]="icons.ChevronsUp" [size]="13" class="text-danger"
                       matTooltip="Urgent priority"></lucide-icon>
          <lucide-icon *ngIf="message.priority === 'Non-urgent'"
                       [img]="icons.ChevronsDown" [size]="13" class="text-faint"
                       matTooltip="Non-urgent priority"></lucide-icon>
        </span>
      </div>
      <div class="msg-meta">
        <span class="msg-date">{{ message.createdAt | date:'M/d/yyyy' }}<span class="msg-date-sep" aria-hidden="true">&#183;</span>{{ message.createdAt | date:'h:mm:ss a' }}</span>
        <span class="msg-size pc-mono">{{ message.size | fileSize }}</span>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      box-sizing: border-box;
    }

    /* fixed height: must stay in step with MessageListComponent's
       messageRowHeight, which drives the virtual scroll item size */
    .msg-item {
      height: 76px;
      box-sizing: border-box;
      padding: 10px 14px;
      border-bottom: 1px solid var(--pc-border-soft);
      border-left: 3px solid transparent;
      background: var(--pc-surface);
      transition: background-color 0.12s ease;
      min-width: 0;
      overflow: hidden;
    }

    .msg-item:hover {
      background: var(--pc-hover);
    }

    .msg-item.msg-selected {
      background: var(--pc-selected);
      border-left-color: var(--pc-selected-edge);
    }

    /* Ticked but not the one on screen: same family as the open row, quieter,
       so it is obvious what Delete (n) is about to take. */
    .msg-item.msg-in-selection {
      background: color-mix(in srgb, var(--pc-selected) 55%, var(--pc-surface));
      border-left-color: color-mix(in srgb, var(--pc-selected-edge) 55%, transparent);
    }

    .msg-subject {
      font-size: 13.5px;
      font-weight: 500;
      color: var(--pc-ink);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      margin-bottom: 3px;
      line-height: 1.35;
    }

    .msg-item.msg-unread .msg-subject {
      font-weight: 700;
      color: var(--pc-ink-strong);
    }

    .msg-item.msg-selected .msg-subject {
      color: var(--pc-ink-strong);
      font-weight: 600;
    }

    .msg-meta {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      font-size: 11.5px;
      color: var(--pc-muted);
      line-height: 1.5;
      min-width: 0;
    }

    .msg-from {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      min-width: 0;
    }

    .msg-date {
      white-space: nowrap;
      color: var(--pc-faint);
    }

    /* a quiet separator so the date and the time read as two facts, not one
       long number -- kept lighter than the text it divides */
    .msg-date-sep {
      display: inline-block;
      padding: 0 0.45em;
      opacity: 0.55;
    }

    .msg-size {
      white-space: nowrap;
      color: var(--pc-faint);
    }

    .msg-indicators {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      flex-shrink: 0;
      color: var(--pc-muted);
    }

    .text-danger { color: var(--pc-danger); }
    .text-faint { color: var(--pc-faint); }
  `]
})
export class MessageListItemComponent {
  protected readonly icons = { Paperclip, ChevronsUp, ChevronsDown };

  @Input() message!: RefDto;
  @Input() selected = false;

  /** Ticked as part of a ctrl/shift selection, but not the message on screen. */
  @Input() inSelection = false;
  @Output() select = new EventEmitter<MouseEvent>();

  constructor(private emailService: EmailService) {}

  onSelect(event: MouseEvent): void {
    this.select.emit(event);
  }

  getFromDisplay(): string {
    return this.emailService.formatEmailAddressList(this.message?.from || []);
  }

  getAttachmentTooltip(): string {
    if (!this.message.attachmentCount || this.message.attachmentCount === 0) {
      return 'No attachments';
    }
    return this.message.attachmentCount === 1 ? 'Has 1 attachment' : `Has ${this.message.attachmentCount} attachments`;
  }
}
