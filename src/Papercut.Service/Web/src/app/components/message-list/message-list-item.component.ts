import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { EmailService } from '../../services/email.service';
import { RefDto } from 'src/app/models';

@Component({
  selector: 'app-message-list-item',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, MatIconModule, FileSizePipe],
  template: `
    <div class="px-4 py-3 border-b border-gray-100 dark:border-gray-700 cursor-pointer transition-colors duration-200 hover:bg-gray-50 dark:hover:bg-gray-700 w-full min-w-0"
         [class.bg-blue-50]="selected"
         [class.dark:bg-blue-900]="selected"
         [class.border-l-4]="selected"
         [class.border-blue-500]="selected"
         [class.dark:border-blue-400]="selected"
         [class.bg-blue-100]="!message.isRead && !selected"
         [class.dark:bg-blue-800]="!message.isRead && !selected"
         [class.border-l-2]="!message.isRead && !selected"
         [class.border-blue-400]="!message.isRead && !selected"
         (click)="onSelect()">
      <div class="font-semibold text-gray-800 dark:text-gray-100 mb-1 overflow-hidden text-ellipsis whitespace-nowrap max-w-full" 
           [class.font-bold]="!message.isRead"
           [matTooltip]="message.subject ?? 'No Subject'">
        {{ message.subject ?? '(No Subject)' }}
      </div>
      <div class="flex justify-between items-center text-xs text-gray-500 dark:text-gray-400">
        <span class="text-gray-600 dark:text-gray-300 flex-1 min-w-0" 
              [class.font-semibold]="!message.isRead">From: {{ getFromDisplay() }}, {{ message.createdAt | date:'short' }}</span>
        <span class="text-gray-600 dark:text-gray-300 font-medium ml-2">{{ message.size | fileSize }}</span>
      </div>
      <div class="flex items-center gap-2 mt-1" *ngIf="hasStatusIndicators()">
        <mat-icon class="text-base text-gray-500 dark:text-gray-400" 
                  *ngIf="message.attachmentCount && message.attachmentCount > 0"
                  matTooltip="Has attachments"
                  style="font-size: 16px; width: 16px; height: 16px;">
          attach_file
        </mat-icon>
        <mat-icon class="text-base text-red-500 dark:text-red-400" 
                  *ngIf="message.priority === 'Urgent'"
                  matTooltip="Urgent priority"
                  style="font-size: 16px; width: 16px; height: 16px;">
          priority_high
        </mat-icon>
        <mat-icon class="text-base text-blue-500 dark:text-blue-400" 
                  *ngIf="message.priority === 'Non-urgent'"
                  matTooltip="Non-urgent priority"
                  style="font-size: 16px; width: 16px; height: 16px;">
          keyboard_arrow_down
        </mat-icon>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      box-sizing: border-box;
    }
    
    mat-icon {
      vertical-align: middle;
    }
  `]
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
    return (this.message?.attachmentCount && this.message.attachmentCount > 0) ||
           this.message?.priority === 'Urgent' ||
           this.message?.priority === 'Non-urgent' || false;
  }
} 