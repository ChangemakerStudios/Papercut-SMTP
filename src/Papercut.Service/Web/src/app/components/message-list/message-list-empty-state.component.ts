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

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

/**
 * Component responsible for displaying the empty state when no messages are available.
 * Extracted from MessageListComponent to follow Single Responsibility Principle.
 * This component provides a consistent empty state display across the application.
 */
@Component({
  selector: 'app-message-list-empty-state',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule
  ],
  template: `
    <div class="flex flex-col items-center justify-center h-full p-8 text-center">
      <mat-icon class="text-6xl mb-4 text-gray-400 dark:text-gray-500 !w-auto !h-auto">inbox</mat-icon>
      <h3 class="text-xl font-medium mb-2 text-gray-700 dark:text-gray-300">No Messages</h3>
      <p class="text-gray-600 dark:text-gray-400">No emails have been received yet</p>
      <p class="text-sm text-gray-500 dark:text-gray-500 mt-2">Messages will appear here when they arrive</p>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListEmptyStateComponent {}
