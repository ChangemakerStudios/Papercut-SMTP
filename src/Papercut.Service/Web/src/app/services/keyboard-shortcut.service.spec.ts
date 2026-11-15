import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';

import { KeyboardShortcutService, ShortcutCommand, comboOf, formatCombo, normalizeCombo } from './keyboard-shortcut.service';

describe('KeyboardShortcutService', () => {
  let service: KeyboardShortcutService;
  let dialog: { openDialogs: unknown[] };
  let cleanup: (() => void)[];

  const press = (key: string, init: KeyboardEventInit = {}, target: EventTarget = document.body): KeyboardEvent => {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init });
    Object.defineProperty(event, 'target', { value: target });
    service.handle(event);
    return event;
  };

  const register = (command: Partial<ShortcutCommand>): jasmine.Spy => {
    const run = jasmine.createSpy(command.id ?? 'run');
    cleanup.push(service.register({ id: 'test', label: 'Test', group: 'App', keys: [], run, ...command }));
    return run;
  };

  beforeEach(() => {
    dialog = { openDialogs: [] };
    cleanup = [];

    TestBed.configureTestingModule({
      providers: [{ provide: MatDialog, useValue: dialog }]
    });

    service = TestBed.inject(KeyboardShortcutService);
  });

  afterEach(() => cleanup.forEach(fn => fn()));

  describe('combos', () => {
    it('drops Shift for symbols, since Shift is how they are typed', () => {
      expect(comboOf({ key: '?', shiftKey: true, ctrlKey: false, altKey: false, metaKey: false })).toBe('?');
      expect(comboOf({ key: '#', shiftKey: true, ctrlKey: false, altKey: false, metaKey: false })).toBe('#');
    });

    it('keeps Shift for letters', () => {
      expect(comboOf({ key: 'J', shiftKey: true, ctrlKey: false, altKey: false, metaKey: false })).toBe('shift+j');
    });

    it('shows bare letters lowercase, so j does not read as Shift+J', () => {
      expect(formatCombo('j')).toBe('j');
      expect(formatCombo('Mod+a')).toMatch(/A$/);
    });

    it('reads Mod as Ctrl off a Mac', () => {
      expect(normalizeCombo('Mod+a')).toBe(/Mac|iPhone|iPad/.test(navigator.userAgent) ? 'meta+a' : 'ctrl+a');
    });
  });

  it('runs the matching command and claims the key', () => {
    const run = register({ keys: ['j'] });

    const event = press('j');

    expect(run).toHaveBeenCalledTimes(1);
    expect(event.defaultPrevented).toBeTrue();
  });

  it('leaves Ctrl+J alone when only j is bound', () => {
    const run = register({ keys: ['j'] });

    press('j', { ctrlKey: true });

    expect(run).not.toHaveBeenCalled();
  });

  it('does nothing while typing in a field', () => {
    const run = register({ keys: ['f'] });
    const input = document.createElement('input');
    document.body.appendChild(input);

    const event = press('f', {}, input);

    expect(run).not.toHaveBeenCalled();
    expect(event.defaultPrevented).toBeFalse();
    input.remove();
  });

  it('does nothing while a dialog is open', () => {
    const run = register({ keys: ['?'] });
    dialog.openDialogs = [{}];

    press('?', { shiftKey: true });

    expect(run).not.toHaveBeenCalled();
  });

  it('leaves the key to the browser when the command is unavailable', () => {
    const run = register({ keys: ['f'], isEnabled: () => false });

    const event = press('f');

    expect(run).not.toHaveBeenCalled();
    expect(event.defaultPrevented).toBeFalse();
  });

  describe('scoped keys', () => {
    let list: HTMLElement;
    let elsewhere: HTMLElement;

    beforeEach(() => {
      list = document.createElement('div');
      elsewhere = document.createElement('div');
      document.body.append(list, elsewhere);
    });

    afterEach(() => {
      list.remove();
      elsewhere.remove();
    });

    it('fire while working in the scope', () => {
      const run = register({ scopedKeys: ['ArrowDown'], scope: () => list });
      service.noteInteraction(list);

      press('ArrowDown');

      expect(run).toHaveBeenCalled();
    });

    it('stay with the browser after a click elsewhere, so the page can scroll', () => {
      const run = register({ scopedKeys: ['ArrowDown'], scope: () => list });
      service.noteInteraction(elsewhere);

      const event = press('ArrowDown');

      expect(run).not.toHaveBeenCalled();
      expect(event.defaultPrevented).toBeFalse();
    });

    it('never fire from inside the message frame, which scrolls with them', () => {
      const run = register({ scopedKeys: ['ArrowDown'], scope: () => list });
      service.noteInteraction(list);

      const event = new KeyboardEvent('keydown', { key: 'ArrowDown', cancelable: true });
      Object.defineProperty(event, 'target', { value: document.body });
      service.handle(event, true);

      expect(run).not.toHaveBeenCalled();
    });

    it('global keys still fire from inside the message frame', () => {
      const run = register({ keys: ['j'], scopedKeys: ['ArrowDown'], scope: () => list });

      const event = new KeyboardEvent('keydown', { key: 'j', cancelable: true });
      Object.defineProperty(event, 'target', { value: document.body });
      service.handle(event, true);

      expect(run).toHaveBeenCalled();
    });
  });

  it('forgets commands when their owner unregisters them', () => {
    const run = jasmine.createSpy('run');
    const unregister = service.register({ id: 'gone', label: 'Gone', group: 'App', keys: ['g'], run });

    unregister();
    press('g');

    expect(run).not.toHaveBeenCalled();
    expect(service.commands.some(c => c.id === 'gone')).toBeFalse();
  });
});
