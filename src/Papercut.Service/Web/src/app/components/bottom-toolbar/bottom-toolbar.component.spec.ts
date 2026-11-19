import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ConfirmService } from '../../services/confirm.service';
import { KeyboardShortcutService } from '../../services/keyboard-shortcut.service';
import { BottomToolbarComponent } from './bottom-toolbar.component';

describe('BottomToolbarComponent', () => {
  let component: BottomToolbarComponent;
  let confirmService: { confirm: jasmine.Spy; confirmDelete: jasmine.Spy };

  beforeEach(() => {
    confirmService = {
      confirm: jasmine.createSpy('confirm').and.returnValue(of(true)),
      confirmDelete: jasmine.createSpy('confirmDelete').and.returnValue(of(true))
    };

    TestBed.configureTestingModule({
      imports: [BottomToolbarComponent],
      providers: [
        { provide: ConfirmService, useValue: confirmService },
        { provide: KeyboardShortcutService, useValue: { register: () => () => {} } }
      ]
    });

    component = TestBed.createComponent(BottomToolbarComponent).componentInstance;
  });

  it('routes Delete through the delete confirmation', () => {
    const emitted = jasmine.createSpy('deleteSelected');
    component.deleteSelected.subscribe(emitted);
    component.selectedMessageCount = 2;

    component.onDeleteSelected();

    expect(confirmService.confirmDelete).toHaveBeenCalledTimes(1);
    expect(confirmService.confirm).not.toHaveBeenCalled();
    expect(emitted).toHaveBeenCalledTimes(1);
  });

  it('routes Delete All through the delete confirmation', () => {
    const emitted = jasmine.createSpy('deleteAll');
    component.deleteAll.subscribe(emitted);
    component.totalMessageCount = 5;

    component.onDeleteAll();

    expect(confirmService.confirmDelete).toHaveBeenCalledTimes(1);
    expect(confirmService.confirm).not.toHaveBeenCalled();
    expect(emitted).toHaveBeenCalledTimes(1);
  });

  it('does not delete when the confirmation is declined', () => {
    confirmService.confirmDelete.and.returnValue(of(false));
    const emitted = jasmine.createSpy('deleteSelected');
    component.deleteSelected.subscribe(emitted);
    component.selectedMessageCount = 1;

    component.onDeleteSelected();

    expect(emitted).not.toHaveBeenCalled();
  });
});
