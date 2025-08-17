import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { EmailService } from '../../services/email.service';
import { RefDto } from 'src/app/models';

@Component({
  selector: 'app-message-list-item',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, MatIconModule, MatProgressSpinnerModule, FileSizePipe, TimeAgoPipe],
  template: `
    <div class="px-4 border-b transition-colors duration-200 w-full min-w-0 h-20 flex flex-col justify-center"
         [ngClass]="{
           'bg-blue-100 dark:bg-blue-900 border-l-4 border-blue-600 dark:border-blue-400 border-b-gray-200 dark:border-b-gray-700': selected,
           'bg-blue-50 dark:bg-blue-800 border-l-2 border-blue-500 dark:border-blue-400 border-b-gray-200 dark:border-b-gray-700': !message.isRead && !selected,
           'border-b-gray-200 dark:border-b-gray-700 hover:bg-gray-100 dark:hover:bg-gray-700': !selected && message.isRead && !isLoading,
           'hover:bg-blue-200 dark:hover:bg-blue-800': selected && !isLoading,
           'hover:bg-blue-100 dark:hover:bg-blue-700': !message.isRead && !selected && !isLoading,
           'cursor-pointer': !isLoading,
           'cursor-not-allowed opacity-60': isLoading
         }"
         (click)="onSelect()">
      <div class="font-semibold mb-1 overflow-hidden text-ellipsis whitespace-nowrap max-w-full" 
           [ngClass]="{
             'text-gray-900 dark:text-gray-100 font-bold': !message.isRead,
             'text-gray-800 dark:text-gray-200': message.isRead,
             'text-blue-800 dark:text-blue-200': selected
           }">
           <!-- [matTooltip]="message.subject ?? 'No Subject'" -->           
        {{ message.subject ?? '(No Subject)' }}
      </div>
      <div class="flex justify-between items-center text-xs">
        <span class="flex-1 min-w-0" 
              [ngClass]="{
                'text-gray-700 dark:text-gray-300 font-semibold': !message.isRead,
                'text-gray-600 dark:text-gray-400': message.isRead,
                'text-blue-700 dark:text-blue-300': selected }"
                [matTooltip]="(message.createdAt | date:'full') ?? 'No data'">
                From: {{ getFromDisplay() }}<br/>
                Received: {{ message.createdAt | timeAgo }}</span>
        <div class="flex flex-row items-center ml-2">
          <!-- Loading indicator first -->
          <mat-spinner *ngIf="isLoadingDetail" 
                      diameter="16" 
                      strokeWidth="2" 
                      class="mr-1 text-blue-600 dark:text-blue-400"></mat-spinner>
          
          <!-- Attachment icon second -->
          <mat-icon class="text-base mr-1" 
                    [ngClass]="{
                      'text-gray-600 dark:text-gray-400': !selected,
                      'text-blue-600 dark:text-blue-400': selected
                    }"
                    *ngIf="message.attachmentCount && message.attachmentCount > 0"
                    matTooltip="Has attachments"
                    style="width: auto; font-size: 8pt;">
            attach_file
          </mat-icon>
          
          <!-- Priority icons -->
          <mat-icon class="text-base mr-1 text-red-600 dark:text-red-400" 
                    *ngIf="message.priority === 'Urgent'"
                    matTooltip="Urgent priority"
                    style="width: auto; font-size: 8pt;">
            priority_high
          </mat-icon>
          <mat-icon class="text-base mr-1 text-blue-600 dark:text-blue-400" 
                    *ngIf="message.priority === 'Non-urgent'"
                    matTooltip="Non-urgent priority"
                    style="width: auto; font-size: 8pt;">
            keyboard_arrow_down
          </mat-icon>
          
          <!-- File size last -->
          <span class="font-medium" 
                [ngClass]="{
                  'text-gray-700 dark:text-gray-300': !message.isRead,
                  'text-gray-600 dark:text-gray-400': message.isRead,
                  'text-blue-700 dark:text-blue-300': selected
                }">{{ message.size | fileSize }}</span>
        </div>
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
  @Input() isLoading = false;
  @Input() isLoadingDetail = false;
  @Output() select = new EventEmitter<void>();

  constructor(private emailService: EmailService) {}

  onSelect(): void {
    if (this.isLoading) {
      return; // Prevent action during loading
    }
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