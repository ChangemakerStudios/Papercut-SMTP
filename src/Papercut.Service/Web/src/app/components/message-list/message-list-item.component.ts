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
    <div class="px-4 py-3 border-b border-gray-100 dark:border-gray-700 cursor-pointer transition-colors duration-200 hover:bg-gray-50 dark:hover:bg-gray-700"
         [class.bg-blue-50]="selected"
         [class.dark:bg-blue-900]="selected"
         [class.border-l-4]="selected"
         [class.border-blue-500]="selected"
         [class.dark:border-blue-400]="selected"
         (click)="onSelect()">
      <div class="font-semibold text-gray-800 dark:text-gray-100 mb-1 truncate" [matTooltip]="message.subject ?? 'No Subject'">
        {{ message.subject ?? '(No Subject)' }}
      </div>
      <div class="flex justify-between items-center text-xs text-gray-500 dark:text-gray-400">
        <span class="text-gray-600 dark:text-gray-300 flex-1 min-w-0">From: {{ getFromDisplay() }}, {{ message.createdAt | date:'short' }}</span>
        <span class="text-gray-600 dark:text-gray-300 font-medium ml-2">{{ message.size | fileSize }}</span>
      </div>
      <div class="flex items-center gap-1 mt-1" *ngIf="hasStatusIndicators()">
        <div class="w-2 h-2 bg-blue-500 rounded-full" *ngIf="!message.isRead" 
             matTooltip="Unread message"></div>
        <div class="w-2 h-2 bg-red-500 rounded-full" *ngIf="message.priority === 'Urgent'" 
             matTooltip="High priority"></div>
        <div class="w-2 h-2 bg-green-500 rounded-full" *ngIf="message.attachments && message.attachments > 0" 
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

  hasStatusIndicators(): boolean {
    return this.message?.isRead === false || 
           this.message?.priority === 'Urgent' || 
           (this.message?.attachments && this.message.attachments > 0) || false;
  }
} 