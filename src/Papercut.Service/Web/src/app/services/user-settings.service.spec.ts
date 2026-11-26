import { UserSettingsService } from './user-settings.service';

describe('UserSettingsService', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  describe('delete confirmation', () => {
    it('is on by default', () => {
      expect(new UserSettingsService().isDeleteConfirmationEnabled()).toBeTrue();
    });

    it('stays off across reloads once turned off', () => {
      new UserSettingsService().setDeleteConfirmationEnabled(false);

      expect(new UserSettingsService().isDeleteConfirmationEnabled()).toBeFalse();
    });

    it('can be turned back on', () => {
      const settings = new UserSettingsService();

      settings.setDeleteConfirmationEnabled(false);
      settings.setDeleteConfirmationEnabled(true);

      expect(new UserSettingsService().isDeleteConfirmationEnabled()).toBeTrue();
    });
  });
});
