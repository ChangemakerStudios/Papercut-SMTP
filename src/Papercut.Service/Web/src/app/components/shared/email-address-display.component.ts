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
import { MatChipsModule } from '@angular/material/chips';
import { EmailListPipe } from '../../pipes/email-list.pipe';

export interface EmailAddress {
  name?: string;
  address: string;
}

/**
 * Component for displaying email addresses in a consistent, styled format.
 * Can be used for From, To, CC, BCC fields and other email address displays.
 * This component focuses solely on email address presentation.
 */
@Component({
  selector: 'app-email-address-display',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatChipsModule,
    EmailListPipe
  ],
  template: `
    <div class="flex items-start gap-2 p-2" [ngClass]="containerClass">
      <!-- Icon -->
      <div class="flex items-center justify-center w-8 h-8 rounded-full flex-shrink-0" [ngClass]="iconClass">
        <mat-icon [ngClass]="iconColorClass">{{ iconName }}</mat-icon>
      </div>
      
      <!-- Content -->
      <div class="flex-1 min-w-0">
        <h4 class="font-semibold text-sm mb-0.5" [ngClass]="labelClass">{{ label }}</h4>
        <div class="text-sm break-words" [ngClass]="contentClass">
          <ng-container *ngIf="emailAddresses && emailAddresses.length > 0; else noAddresses">
            <ng-container *ngIf="emailAddresses.length === 1; else multipleAddresses">
              <!-- Single email address -->
              <div class="flex items-center gap-2">
                <span class="font-medium">{{ emailAddresses[0].name || emailAddresses[0].address }}</span>
                <span *ngIf="emailAddresses[0].name" class="text-gray-500 dark:text-gray-400">
                  &lt;{{ emailAddresses[0].address }}&gt;
                </span>
              </div>
            </ng-container>
            
            <ng-template #multipleAddresses>
              <!-- Multiple email addresses -->
              <div class="space-y-1">
                <div *ngFor="let email of emailAddresses" class="flex items-center gap-2">
                  <span class="font-medium">{{ email.name || email.address }}</span>
                  <span *ngIf="email.name" class="text-gray-500 dark:text-gray-400">
                    &lt;{{ email.address }}&gt;
                  </span>
                </div>
              </div>
            </ng-template>
          </ng-container>
          
          <ng-template #noAddresses>
            <span class="text-gray-500 dark:text-gray-400 italic">No addresses</span>
          </ng-template>
        </div>
      </div>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailAddressDisplayComponent {
  @Input() emailAddresses: EmailAddress[] = [];
  @Input() label: string = 'Email Addresses';
  @Input() type: 'from' | 'to' | 'cc' | 'bcc' = 'to';
  @Input() showIcon: boolean = true;
  @Input() clickable: boolean = false;

  @Output() emailClick = new EventEmitter<EmailAddress>();

  get iconName(): string {
    switch (this.type) {
      case 'from': return 'person';
      case 'to': return 'people';
      case 'cc': return 'people_outline';
      case 'bcc': return 'visibility_off';
      default: return 'email';
    }
  }

  get containerClass(): string {
    switch (this.type) {
      case 'from': return 'bg-blue-50 dark:bg-blue-900/20';
      case 'to': return 'bg-green-50 dark:bg-green-900/20';
      case 'cc': return 'bg-yellow-50 dark:bg-yellow-900/20';
      case 'bcc': return 'bg-red-50 dark:bg-red-900/20';
      default: return 'bg-gray-50 dark:bg-gray-900/20';
    }
  }

  get iconClass(): string {
    switch (this.type) {
      case 'from': return 'bg-blue-100 dark:bg-blue-800/40';
      case 'to': return 'bg-green-100 dark:bg-green-800/40';
      case 'cc': return 'bg-yellow-100 dark:bg-yellow-800/40';
      case 'bcc': return 'bg-red-100 dark:bg-red-800/40';
      default: return 'bg-gray-100 dark:bg-gray-800/40';
    }
  }

  get iconColorClass(): string {
    switch (this.type) {
      case 'from': return 'text-blue-600 dark:text-blue-400';
      case 'to': return 'text-green-600 dark:text-green-400';
      case 'cc': return 'text-yellow-600 dark:text-yellow-400';
      case 'bcc': return 'text-red-600 dark:text-red-400';
      default: return 'text-gray-600 dark:text-gray-400';
    }
  }

  get labelClass(): string {
    return 'text-gray-800 dark:text-gray-100';
  }

  get contentClass(): string {
    return 'text-gray-700 dark:text-gray-300';
  }

  onEmailClick(email: EmailAddress): void {
    if (this.clickable) {
      this.emailClick.emit(email);
    }
  }
}
