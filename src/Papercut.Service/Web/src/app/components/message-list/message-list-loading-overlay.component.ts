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

import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Component responsible for displaying loading overlays in the message list.
 * Extracted from MessageListComponent to follow Single Responsibility Principle.
 * This component provides consistent loading state displays across the application.
 */
@Component({
  selector: 'app-message-list-loading-overlay',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div *ngIf="isLoading" class="absolute inset-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm z-10 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3">
        <mat-spinner diameter="40" strokeWidth="4"></mat-spinner>
        <span class="text-sm font-medium text-gray-600 dark:text-gray-300">{{ loadingMessage }}</span>
      </div>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.Default
})
export class MessageListLoadingOverlayComponent {
  @Input() isLoading: boolean = false;
  @Input() loadingMessage: string = 'Loading message...';
}
