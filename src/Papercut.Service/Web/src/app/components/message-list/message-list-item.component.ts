import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailService } from '../../services/email.service';
import { RefDto } from 'src/app/models';

@Component({
  selector: 'app-message-list-item',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, FileSizePipe],
  template: `
    <div class="message-item"
         [class.selected]="selected"
         (click)="onSelect()">
      <div class="message-subject" [matTooltip]="message.subject ?? 'No Subject'">
        {{ message.subject ?? '(No Subject)' }}
      </div>
      <div class="message-from" [matTooltip]="getFromDisplay()">
        {{ getFromDisplay() }}
      </div>      
      <div class="message-meta">
        <span>{{ message.createdAt| date:'MMM d, y h:mm a' }}</span>
        <span>•</span>
        <span>{{ message.size | fileSize }}</span>
      </div>
      <div class="message-status-indicators">
        <div class="message-indicator unread" *ngIf="!message.isRead" 
             matTooltip="Unread message"></div>
        <div class="message-indicator important" *ngIf="message.priority === 'high'" 
             matTooltip="High priority"></div>
        <div class="message-indicator has-attachments" *ngIf="message.hasAttachments" 
             matTooltip="Has attachments"></div>
      </div>
    </div>
  `
})
export class MessageListItemComponent {
  @Input() message!: RefDto;
  @Input() selected = false;
  @Output() select = new EventEmitter<void>();

  constructor(private emailService: EmailService) {}

  onSelect(): void {
    console.log('Message item clicked:', this.message?.id);
    this.select.emit();
  }

  getFromDisplay(): string {
    return this.emailService.formatEmailAddressList(this.message?.from || []);
  }
} 