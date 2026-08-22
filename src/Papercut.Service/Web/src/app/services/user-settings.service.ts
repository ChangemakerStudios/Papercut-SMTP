import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type MessageSortOrder = 'desc' | 'asc';

const SORT_ORDER_KEY = 'papercut-sort-order';
const NOTIFICATIONS_KEY = 'papercut-notifications-enabled';

/**
 * Client-side user preferences (persisted in localStorage), covering the
 * desktop Options fields that are per-viewer rather than per-server:
 * message sort order and new-mail notifications.
 */
@Injectable({ providedIn: 'root' })
export class UserSettingsService {
  private sortOrder = new BehaviorSubject<MessageSortOrder>(this.loadSortOrder());
  readonly sortOrder$ = this.sortOrder.asObservable();

  private notificationsEnabled = new BehaviorSubject<boolean>(this.loadNotificationsEnabled());
  readonly notificationsEnabled$ = this.notificationsEnabled.asObservable();

  getSortOrder(): MessageSortOrder {
    return this.sortOrder.value;
  }

  setSortOrder(order: MessageSortOrder): void {
    if (this.sortOrder.value !== order) {
      localStorage.setItem(SORT_ORDER_KEY, order);
      this.sortOrder.next(order);
    }
  }

  areNotificationsEnabled(): boolean {
    return this.notificationsEnabled.value;
  }

  setNotificationsEnabled(enabled: boolean): void {
    if (this.notificationsEnabled.value !== enabled) {
      localStorage.setItem(NOTIFICATIONS_KEY, String(enabled));
      this.notificationsEnabled.next(enabled);
    }
  }

  private loadSortOrder(): MessageSortOrder {
    return localStorage.getItem(SORT_ORDER_KEY) === 'asc' ? 'asc' : 'desc';
  }

  private loadNotificationsEnabled(): boolean {
    // Enabled by default, matching the desktop's ShowNotifications
    return localStorage.getItem(NOTIFICATIONS_KEY) !== 'false';
  }
}
