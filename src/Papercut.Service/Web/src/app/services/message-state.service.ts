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
}
