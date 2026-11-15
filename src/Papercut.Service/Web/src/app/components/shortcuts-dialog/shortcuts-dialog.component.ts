// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { Component } from '@angular/core';

import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { LucideAngularModule, Keyboard, X } from 'lucide-angular';
import {
  KeyboardShortcutService,
  ShortcutGroup,
  formatCombo
} from '../../services/keyboard-shortcut.service';

interface ShortcutRow {
  label: string;
  keys: string[];
}

/**
 * The "?" cheat sheet. Built from whatever is registered right now, so it
 * never lists a shortcut that does not exist (or misses one that does).
 */
@Component({
  selector: 'app-shortcuts-dialog',
  imports: [MatDialogModule, LucideAngularModule],
  template: `
    <div class="shortcuts-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="icons.Keyboard" [size]="16"></lucide-icon>
        <h2 class="dialog-title">Keyboard shortcuts</h2>
        <button class="dialog-close" (click)="close()" aria-label="Close">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>

      <div class="dialog-body">
        @for (group of groups; track group.name) {
          <section>
            <h3 class="group-title">{{ group.name }}</h3>
            @for (row of group.rows; track row.label) {
              <div class="shortcut-row">
                <span class="shortcut-label">{{ row.label }}</span>
                <span class="shortcut-keys">
                  @for (key of row.keys; track key; let last = $last) {
                    <kbd>{{ key }}</kbd>
                    @if (!last) { <span class="or">or</span> }
                  }
                </span>
              </div>
            }
          </section>
        }
        <p class="hint">Shortcuts pause while you're typing in a field.</p>
      </div>
    </div>
  `,
  styles: [`
    .shortcuts-dialog {
      min-width: 360px;
      max-width: 460px;
      background: var(--pc-surface);
      color: var(--pc-ink);
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 14px 18px 6px;
      color: var(--pc-accent-text);
    }

    .dialog-title {
      font-size: var(--pc-text-title);
      font-weight: 700;
      color: var(--pc-ink-strong);
      margin: 0 auto 0 0;
    }

    .dialog-close {
      display: inline-flex;
      padding: 5px;
      border-radius: 6px;
      border: 1px solid transparent;
      background: transparent;
      color: var(--pc-muted);
      cursor: pointer;
    }

    .dialog-close:hover { background: var(--pc-hover); color: var(--pc-ink); }

    .dialog-body {
      padding: 4px 18px 16px;
      max-height: 70vh;
      overflow-y: auto;
    }

    .group-title {
      margin: 12px 0 4px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--pc-muted);
    }

    .shortcut-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 5px 0;
      border-bottom: 1px solid var(--pc-border);
      font-size: var(--pc-text-ui);
    }

    .shortcut-keys {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      flex-shrink: 0;
    }

    kbd {
      display: inline-block;
      min-width: 22px;
      padding: 1px 6px;
      font-family: var(--pc-font-mono);
      font-size: 11.5px;
      text-align: center;
      color: var(--pc-ink);
      background: var(--pc-surface-2);
      border: 1px solid var(--pc-border);
      border-bottom-width: 2px;
      border-radius: 4px;
    }

    .or {
      font-size: 11px;
      color: var(--pc-muted);
    }

    .hint {
      margin: 12px 0 0;
      font-size: var(--pc-text-small);
      color: var(--pc-muted);
    }
  `]
})
export class ShortcutsDialogComponent {
  protected readonly icons = { Keyboard, X };

  private static readonly ORDER: ShortcutGroup[] = ['Messages', 'Open message', 'App'];

  /** Registration order follows whichever component loaded first; read moving around first instead. */
  private static readonly ROW_ORDER = ['list.next', 'list.previous', 'list.search', 'messages.delete', 'message.forward'];

  private static rank(id: string): number {
    const index = ShortcutsDialogComponent.ROW_ORDER.indexOf(id);
    return index === -1 ? ShortcutsDialogComponent.ROW_ORDER.length : index;
  }

  readonly groups: { name: ShortcutGroup; rows: ShortcutRow[] }[];

  constructor(
    private dialogRef: MatDialogRef<ShortcutsDialogComponent>,
    shortcuts: KeyboardShortcutService
  ) {
    const rows = (group: ShortcutGroup): ShortcutRow[] => shortcuts.commands
      .filter(c => c.group === group)
      .sort((a, b) => ShortcutsDialogComponent.rank(a.id) - ShortcutsDialogComponent.rank(b.id))
      .map(c => ({ label: c.label, keys: c.keys.map(formatCombo) }));

    this.groups = ShortcutsDialogComponent.ORDER
      .map(name => ({
        name,
        rows: name === 'App'
          ? [...rows(name), { label: 'Close a pop-up, like this one', keys: [formatCombo('Escape')] }]
          : rows(name)
      }))
      .filter(group => group.rows.length > 0);
  }

  close(): void {
    this.dialogRef.close();
  }
}
