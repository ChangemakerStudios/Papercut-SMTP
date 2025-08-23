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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaginationComponent } from '../pagination/pagination.component';

/**
 * Component responsible for displaying the message list header with pagination controls.
 * Extracted from MessageListComponent to follow Single Responsibility Principle.
 * This component handles pagination display and delegates pagination actions to the parent.
 */
@Component({
  selector: 'app-message-list-header',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    PaginationComponent
  ],
  template: `
    <div class="w-full min-w-0">
      <app-pagination
        [pageSize]="pageSize"
        [pageStart]="pageStart"
        [currentPage]="currentPage"
        [totalPages]="totalPages"
        [totalCount]="totalCount"
        [pageSizeOptions]="pageSizeOptions"
        [isLoading]="isLoading"
        (pageSizeChange)="pageSizeChange.emit($event)"
        (pageChange)="pageChange.emit($event)">
      </app-pagination>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListHeaderComponent {
  @Input() pageSize: number = 10;
  @Input() pageStart: number = 0;
  @Input() currentPage: number = 1;
  @Input() totalPages: number = 1;
  @Input() totalCount: number = 0;
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];
  @Input() isLoading: boolean = false;

  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() pageChange = new EventEmitter<number>();
}
