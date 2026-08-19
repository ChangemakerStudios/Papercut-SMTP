import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export interface PaginationInfo {
  currentPage: number;
  totalPages: number;
  limit: number;
  start: number;
  totalCount: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule, MatProgressSpinnerModule],
  template: `
    <div class="pager flex items-center justify-between gap-2 p-2 min-w-0">
      <div class="text-xs text-muted flex-shrink-0 flex items-center gap-2">
        <ng-container *ngIf="totalCount > 0; else noResults">
          {{ pageStart + 1 }}–{{ displayEnd }} of {{ totalCount }}
        </ng-container>
        <ng-template #noResults>
          No results
        </ng-template>
        <mat-spinner *ngIf="isLoading" diameter="12" strokeWidth="2"></mat-spinner>
      </div>
      <div class="flex items-center gap-1 flex-shrink-0">
        <select class="pager-select"
                [ngModel]="pageSize"
                (ngModelChange)="onPageSizeChange($event)"
                [disabled]="isLoading">
          <option *ngFor="let size of pageSizeOptions" [value]="size">{{ size }}/page</option>
        </select>
        <div class="flex items-center gap-1 flex-shrink-0">
          <button class="pager-btn" (click)="goToPage(1)" [disabled]="currentPage === 1 || isLoading">«</button>
          <button class="pager-btn" (click)="prevPage()" [disabled]="currentPage === 1 || isLoading">‹</button>
          <ng-container *ngFor="let p of visiblePageNumbers">
            <button *ngIf="p > 0; else dots"
                    class="pager-btn"
                    [class.pager-btn-active]="p === currentPage"
                    (click)="goToPage(p)"
                    [disabled]="isLoading">{{ p }}</button>
            <ng-template #dots>
              <span class="px-1 text-faint flex-shrink-0">…</span>
            </ng-template>
          </ng-container>
          <button class="pager-btn" (click)="nextPage()" [disabled]="currentPage === totalPages || isLoading">›</button>
          <button class="pager-btn" (click)="goToPage(totalPages)" [disabled]="currentPage === totalPages || isLoading">»</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      min-width: 0;
    }

    :host > div {
      width: 100%;
      min-width: 0;
    }

    .pager {
      border-top: 1px solid var(--pc-border);
      background: var(--pc-surface-2);
    }

    .pager-select {
      font-size: 11px;
      padding: 3px 6px;
      border-radius: 5px;
      border: 1px solid var(--pc-border);
      background: var(--pc-surface);
      color: var(--pc-ink);
      min-width: 0;
    }

    .pager-select:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .pager-btn {
      min-width: 22px;
      padding: 3px 5px;
      font-size: 11px;
      border-radius: 5px;
      border: 1px solid var(--pc-border);
      background: var(--pc-surface);
      color: var(--pc-ink);
      flex-shrink: 0;
      cursor: pointer;
      transition: background-color 0.12s ease;
    }

    .pager-btn:hover:not(:disabled):not(.pager-btn-active) {
      background: var(--pc-hover);
    }

    .pager-btn:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }

    .pager-btn-active {
      background: var(--pc-accent);
      border-color: var(--pc-accent);
      color: var(--pc-on-chrome);
      font-weight: 600;
    }
  `]
})
export class PaginationComponent implements OnChanges {
  @Input() pageSize = 10;
  @Input() pageStart = 0;
  @Input() currentPage = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];
  @Input() isLoading = false;

  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() pageChange = new EventEmitter<number>();

  visiblePageNumbers: (number | 0)[] = [1]; // 0 denotes ellipsis

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentPage'] || changes['totalPages']) {
      this.visiblePageNumbers = this.computeVisiblePages(this.currentPage, this.totalPages);
    }
  }

  get displayEnd(): number {
    return Math.min(this.pageStart + this.pageSize, this.totalCount);
  }

  onPageSizeChange(size: number): void {
    this.pageSizeChange.emit(Number(size) || 10);
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  goToPage(page: number): void {
    const safePage = Math.min(Math.max(1, page), this.totalPages || 1);
    this.pageChange.emit(safePage);
  }

  private computeVisiblePages(current: number, total: number): (number | 0)[] {
    const pages: (number | 0)[] = [];
    const window = 1;
    if (total <= 7) {
      for (let p = 1; p <= total; p++) pages.push(p);
      return pages;
    }
    pages.push(1);
    if (current > 2 + window) pages.push(0);
    for (let p = Math.max(2, current - window); p <= Math.min(total - 1, current + window); p++) {
      pages.push(p);
    }
    if (current < total - (1 + window)) pages.push(0);
    pages.push(total);
    return pages;
  }
}
