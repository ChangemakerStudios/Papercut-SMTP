import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

/**
 * Shared message UI state: which message is open, how many exist, and a
 * refresh channel so actions taken elsewhere (toolbar deletes, dialogs)
 * can tell the list to reload.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageStateService {
  private currentMessageId = new BehaviorSubject<string | null>(null);
  readonly currentMessageId$ = this.currentMessageId.asObservable();

  private totalCount = new BehaviorSubject<number>(0);
  readonly totalCount$ = this.totalCount.asObservable();

  private refreshRequests = new Subject<void>();
  readonly refreshRequests$ = this.refreshRequests.asObservable();

  /** Carries the ids of messages that were just deleted, so the list can put
   *  the selection somewhere sensible instead of dropping it. */
  private messageDeleted = new Subject<string[]>();
  readonly messageDeleted$ = this.messageDeleted.asObservable();

  /**
   * Every message ticked in the list, which is what the toolbar's Delete acts
   * on. Distinct from currentMessageId: that is the one message on screen, and
   * with ctrl/shift selection it is only ever one member of this set.
   */
  private selectedIds = new BehaviorSubject<string[]>([]);
  readonly selectedIds$ = this.selectedIds.asObservable();

  setCurrentMessageId(id: string | null): void {
    if (this.currentMessageId.value !== id) {
      this.currentMessageId.next(id);
    }
  }

  getCurrentMessageId(): string | null {
    return this.currentMessageId.value;
  }

  setTotalCount(count: number): void {
    if (this.totalCount.value !== count) {
      this.totalCount.next(count);
    }
  }

  requestRefresh(): void {
    this.refreshRequests.next();
  }

  notifyDeleted(ids: string[]): void {
    this.messageDeleted.next(ids);
  }

  setSelectedIds(ids: string[]): void {
    this.selectedIds.next(ids);
  }

  getSelectedIds(): string[] {
    return this.selectedIds.value;
  }
}
