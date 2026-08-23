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

import { LucideAngularModule, MailOpen } from 'lucide-angular';

/**
 * Component responsible for displaying the state when no message is selected.
 * Extracted from MessageListComponent to follow Single Responsibility Principle.
 * This component provides a consistent no-selection state display.
 */
@Component({
  selector: 'app-message-list-no-selection',
  imports: [
    LucideAngularModule
],
  template: `
    <div class="flex-1 flex flex-col items-center justify-center p-8">
      <lucide-icon [img]="icons.MailOpen" [size]="48" [strokeWidth]="1.5" class="mb-4 text-faint"></lucide-icon>
      <h3 class="text-lg font-semibold mb-2 text-ink-strong">No message selected</h3>
      <p class="text-muted text-sm">Select a message from the list to view its contents</p>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListNoSelectionComponent {
  protected readonly icons = { MailOpen };
}
