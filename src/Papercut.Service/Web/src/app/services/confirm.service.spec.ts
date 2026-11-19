import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';

import { ConfirmDialogData, ConfirmDialogResult } from '../components/confirm-dialog/confirm-dialog.component';
import { ConfirmService } from './confirm.service';
import { UserSettingsService } from './user-settings.service';

describe('ConfirmService', () => {
  let service: ConfirmService;
  let userSettings: UserSettingsService;
  let dialog: { open: jasmine.Spy };
  let closed: Subject<ConfirmDialogResult | undefined>;

  const data: ConfirmDialogData = { title: 'Delete Message', message: 'Delete this message?' };

  const openedWith = (): ConfirmDialogData => dialog.open.calls.mostRecent().args[1].data;

  const close = (result?: ConfirmDialogResult): void => {
    closed.next(result);
    closed.complete();
  };

  beforeEach(() => {
    localStorage.clear();

    closed = new Subject();
    dialog = { open: jasmine.createSpy('open').and.callFake(() => ({ afterClosed: () => closed })) };

    TestBed.configureTestingModule({
      providers: [{ provide: MatDialog, useValue: dialog }]
    });

    service = TestBed.inject(ConfirmService);
    userSettings = TestBed.inject(UserSettingsService);
  });

  afterEach(() => localStorage.clear());

  describe('confirm', () => {
    it('emits true only when confirmed', () => {
      const results: boolean[] = [];

      service.confirm(data).subscribe(r => results.push(r));
      close({ confirmed: true, dontAskAgain: false });

      expect(results).toEqual([true]);
    });

    it('emits false when dismissed without a result', () => {
      const results: boolean[] = [];

      service.confirm(data).subscribe(r => results.push(r));
      close(undefined);

      expect(results).toEqual([false]);
    });

    it('does not offer the never-ask checkbox', () => {
      service.confirm(data).subscribe();

      expect(openedWith().dontAskAgainLabel).toBeUndefined();
    });
  });

  describe('confirmDelete', () => {
    it('asks, with the never-ask checkbox, while confirmation is on', () => {
      service.confirmDelete(data).subscribe();

      expect(dialog.open).toHaveBeenCalledTimes(1);
      expect(openedWith().dontAskAgainLabel).toBe('Never ask for confirmation on delete');
    });

    it('turns confirmation off when confirmed with the checkbox ticked', () => {
      const results: boolean[] = [];

      service.confirmDelete(data).subscribe(r => results.push(r));
      close({ confirmed: true, dontAskAgain: true });

      expect(results).toEqual([true]);
      expect(userSettings.isDeleteConfirmationEnabled()).toBeFalse();
    });

    it('keeps confirmation on when confirmed with the checkbox clear', () => {
      service.confirmDelete(data).subscribe();
      close({ confirmed: true, dontAskAgain: false });

      expect(userSettings.isDeleteConfirmationEnabled()).toBeTrue();
    });

    it('keeps confirmation on when cancelled with the checkbox ticked', () => {
      const results: boolean[] = [];

      service.confirmDelete(data).subscribe(r => results.push(r));
      close({ confirmed: false, dontAskAgain: true });

      expect(results).toEqual([false]);
      expect(userSettings.isDeleteConfirmationEnabled()).toBeTrue();
    });

    it('confirms straight away, without a dialog, once confirmation is off', () => {
      userSettings.setDeleteConfirmationEnabled(false);
      const results: boolean[] = [];

      service.confirmDelete(data).subscribe(r => results.push(r));

      expect(dialog.open).not.toHaveBeenCalled();
      expect(results).toEqual([true]);
    });

    it('asks again once confirmation is turned back on', () => {
      userSettings.setDeleteConfirmationEnabled(false);
      userSettings.setDeleteConfirmationEnabled(true);

      service.confirmDelete(data).subscribe();

      expect(dialog.open).toHaveBeenCalledTimes(1);
    });
  });
});
