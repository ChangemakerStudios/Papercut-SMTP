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
 * Component responsible for displaying the state when no message is selected.
 * Extracted from MessageListComponent to follow Single Responsibility Principle.
 * This component provides a consistent no-selection state display.
 */
@Component({
  selector: 'app-message-list-no-selection',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule
  ],
  template: `
    <div class="flex-1 flex flex-col items-center justify-center p-8">
      <mat-icon class="text-6xl mb-4 text-gray-400 dark:text-gray-500 !w-auto !h-auto">email</mat-icon>
      <h3 class="text-xl font-medium mb-2 text-gray-700 dark:text-gray-300">No message selected</h3>
      <p class="text-gray-600 dark:text-gray-400">Select a message from the list to view its contents</p>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListNoSelectionComponent {}
