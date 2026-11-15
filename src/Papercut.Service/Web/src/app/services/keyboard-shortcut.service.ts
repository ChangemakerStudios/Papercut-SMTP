import { Injectable, NgZone } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { BehaviorSubject } from 'rxjs';

export type ShortcutGroup = 'Messages' | 'Open message' | 'App';

/**
 * One action the keyboard can trigger. Components register these while they
 * are on screen; the cheat sheet (and later the command bar) list whatever is
 * registered, so an action and its shortcut are defined in one place.
 */
export interface ShortcutCommand {
  id: string;
  label: string;
  group: ShortcutGroup;

  /** Fire wherever focus is (except while typing). "Mod" is Ctrl, or Cmd on a Mac. */
  keys: string[];

  /** False leaves the key to the browser: nothing open to forward, tab disabled. */
  isEnabled?: () => boolean;

  run: () => void;
}

const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.userAgent);

const TYPING_SELECTOR = 'input, textarea, select, [contenteditable=""], [contenteditable="true"]';

/** "Mod+a" -> "ctrl+a" on Windows, "meta+a" on a Mac; letters lower-cased. */
export function normalizeCombo(combo: string): string {
  return combo
    .split('+')
    .map(part => part === 'Mod' ? (isMac ? 'Meta' : 'Ctrl') : part)
    .map(part => part.toLowerCase())
    .join('+');
}

/**
 * The combo a key press spells, in normalizeCombo's form. Shift is dropped
 * for symbols, since it is how "?" and "#" are typed in the first place.
 */
export function comboOf(event: Pick<KeyboardEvent, 'key' | 'ctrlKey' | 'altKey' | 'shiftKey' | 'metaKey'>): string {
  const key = event.key.toLowerCase();
  const isSymbol = event.key.length === 1 && !/[a-z]/i.test(event.key);

  const parts: string[] = [];
  if (event.ctrlKey) parts.push('ctrl');
  if (event.altKey) parts.push('alt');
  if (event.shiftKey && !isSymbol) parts.push('shift');
  if (event.metaKey) parts.push('meta');
  parts.push(key);

  return parts.join('+');
}

/** "Mod+," -> "Ctrl+," (or "⌘,"); "Delete" -> "Del". For display only. */
export function formatCombo(combo: string): string {
  const names: Record<string, string> = {
    Mod: isMac ? '⌘' : 'Ctrl',
    Delete: 'Del',
    Backspace: isMac ? '⌫' : 'Backspace',
    Escape: 'Esc'
  };

  const parts = combo.split('+');

  // a bare "J" reads as Shift+J; capitals only beside a modifier (Ctrl+A)
  if (parts.length === 1 && /^[a-z]$/i.test(combo)) return combo.toLowerCase();

  return parts
    .map(part => names[part] ?? (part.length === 1 ? part.toUpperCase() : part))
    .join(isMac ? '' : '+');
}

@Injectable({
  providedIn: 'root'
})
export class KeyboardShortcutService {
  private readonly registered = new BehaviorSubject<ShortcutCommand[]>([]);
  readonly commands$ = this.registered.asObservable();

  constructor(private zone: NgZone, private dialog: MatDialog) {
    this.zone.runOutsideAngular(() => {
      document.addEventListener('keydown', event => this.handle(event));
    });
  }

  get commands(): ShortcutCommand[] {
    return this.registered.value;
  }

  /** Adds commands; call the returned function to remove them again. */
  register(...commands: ShortcutCommand[]): () => void {
    const ids = new Set(commands.map(c => c.id));
    this.registered.next([...this.registered.value.filter(c => !ids.has(c.id)), ...commands]);

    return () => this.registered.next(this.registered.value.filter(c => !commands.includes(c)));
  }

  /**
   * Handles a key press from this document, or one relayed from the message
   * frame -- clicking into an email moves focus there, and its key presses
   * would otherwise never reach us.
   */
  handle(event: KeyboardEvent): void {
    if (event.defaultPrevented || event.isComposing) return;
    if (event.repeat && !this.isNavigationKey(event)) return;

    const target = event.target as Element | null;
    if (target?.closest?.(TYPING_SELECTOR)) return;

    // an open dialog owns the keyboard; it closes itself on Esc
    if (this.dialog.openDialogs.length > 0) return;

    const combo = comboOf(event);

    for (const command of this.registered.value) {
      if (!command.keys.some(k => normalizeCombo(k) === combo)) continue;
      if (command.isEnabled && !command.isEnabled()) continue;

      event.preventDefault();
      this.zone.run(() => command.run());
      return;
    }
  }

  /** Holding j or k keeps moving, the way it does in a list. */
  private isNavigationKey(event: KeyboardEvent): boolean {
    return ['j', 'k'].includes(event.key);
  }
}
