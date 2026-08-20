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

import { Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, ScrollText, X, ArrowDownToLine, Pause, Play } from 'lucide-angular';
import { Subscription, timer } from 'rxjs';
import { EnvironmentService } from '../../services/environment.service';

interface LogEntry {
  seq: number;
  timestamp: string;
  level: string;
  message: string;
  exception?: string | null;
}

interface LogTailResponse {
  entries: LogEntry[];
  lastSeq: number;
}

const LEVEL_RANK: Record<string, number> = {
  Verbose: 0, Debug: 1, Information: 2, Warning: 3, Error: 4, Fatal: 5
};

/**
 * Live service log viewer (the desktop app's log window). Tails the
 * service's in-memory log buffer over HTTP, polling while open.
 */
@Component({
  selector: 'app-log-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatProgressSpinnerModule, MatTooltipModule, LucideAngularModule],
  template: `
    <div class="log-dialog">
      <div class="dialog-header">
        <lucide-icon [img]="icons.ScrollText" [size]="16"></lucide-icon>
        <h2 class="dialog-title">Service Log</h2>

        <select class="pc-input level-select" [(ngModel)]="minLevel">
          <option value="Verbose">Verbose</option>
          <option value="Debug">Debug</option>
          <option value="Information">Info</option>
          <option value="Warning">Warning</option>
          <option value="Error">Error</option>
        </select>

        <input class="pc-input filter-input" [(ngModel)]="filterText" placeholder="filter…" />

        <button class="header-btn" (click)="togglePause()"
                [matTooltip]="isPaused ? 'Resume tailing' : 'Pause tailing'">
          <lucide-icon [img]="isPaused ? icons.Play : icons.Pause" [size]="15"></lucide-icon>
        </button>

        <button class="header-btn" [class.active]="follow" (click)="follow = !follow"
                matTooltip="Follow (auto-scroll)">
          <lucide-icon [img]="icons.ArrowDownToLine" [size]="15"></lucide-icon>
        </button>

        <button class="dialog-close" (click)="close()">
          <lucide-icon [img]="icons.X" [size]="16"></lucide-icon>
        </button>
      </div>

      <div class="log-body" #logBody>
        <div class="log-loading" *ngIf="isLoading">
          <mat-spinner diameter="28" strokeWidth="3"></mat-spinner>
          <span>Loading the log…</span>
        </div>
        <div class="log-empty" *ngIf="!isLoading && visibleEntries().length === 0">
          {{ entries.length === 0 ? 'Waiting for log output…' : 'No entries match the current filter.' }}
        </div>
        <div class="log-line" *ngFor="let entry of visibleEntries(); trackBy: trackBySeq">
          <span class="log-time">{{ entry.timestamp | date:'HH:mm:ss.SSS' }}</span>
          <span class="log-level" [ngClass]="levelClass(entry.level)">[{{ levelAbbr(entry.level) }}]</span>
          <span class="log-msg">{{ entry.message }}<ng-container *ngIf="entry.exception">
{{ entry.exception }}</ng-container></span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .log-dialog {
      display: flex;
      flex-direction: column;
      width: 80vw;
      max-width: 1100px;
      height: 70vh;
      background: var(--pc-surface);
      color: var(--pc-ink);
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
      border-bottom: 1px solid var(--pc-border);
      color: var(--pc-accent-text);
      flex-shrink: 0;
    }

    .dialog-title {
      font-size: 15px;
      font-weight: 700;
      color: var(--pc-ink-strong);
      margin: 0 auto 0 0;
    }

    .pc-input {
      height: 28px;
      padding: 0 8px;
      font-size: 12px;
      font-family: inherit;
      color: var(--pc-ink);
      background: var(--pc-surface);
      border: 1px solid var(--pc-border);
      border-radius: 6px;
      outline: none;
    }

    .pc-input:focus { border-color: var(--pc-accent); }

    .level-select { width: 100px; }
    .filter-input { width: 180px; font-family: var(--pc-font-mono); }

    .header-btn, .dialog-close {
      display: inline-flex;
      padding: 5px;
      border-radius: 6px;
      border: 1px solid transparent;
      background: transparent;
      color: var(--pc-muted);
      cursor: pointer;
    }

    .header-btn:hover, .dialog-close:hover { background: var(--pc-hover); color: var(--pc-ink); }

    .header-btn.active {
      color: var(--pc-accent-text);
      background: var(--pc-accent-soft);
    }

    .log-body {
      flex: 1;
      overflow-y: auto;
      padding: 8px 0;
      background: var(--pc-surface);
      font-family: var(--pc-font-mono);
      /* a log is scanned, not read: one step down the scale and tighter
         leading fits noticeably more of it on screen */
      font-size: var(--pc-text-small);
      line-height: var(--pc-leading-ui);
    }

    .log-empty {
      padding: 30px;
      text-align: center;
      font-family: var(--pc-font-sans);
      font-size: 12.5px;
      color: var(--pc-muted);
    }

    .log-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 40px;
      font-family: var(--pc-font-sans);
      font-size: 12.5px;
      color: var(--pc-muted);
    }

    .log-line {
      display: flex;
      gap: 10px;
      padding: 0 16px;
      align-items: baseline;
    }

    .log-line:hover { background: var(--pc-hover); }

    .log-time {
      flex-shrink: 0;
      color: var(--pc-faint);
    }

    .log-level {
      flex-shrink: 0;
      width: 42px;
      font-weight: 700;
    }

    /* Each level gets its own distinct color */
    .level-vrb { color: var(--pc-faint); }
    .level-dbg { color: var(--pc-muted); }
    .level-inf { color: var(--pc-ok); }
    .level-wrn { color: var(--pc-warn); }
    .level-err { color: var(--pc-danger); }
    .level-ftl { color: var(--pc-danger-strong); text-decoration: underline; }

    .log-msg {
      flex: 1;
      min-width: 0;
      white-space: pre-wrap;
      word-break: break-word;
      color: var(--pc-ink);
    }
  `]
})
export class LogDialogComponent implements OnDestroy {
  protected readonly icons = { ScrollText, X, ArrowDownToLine, Pause, Play };

  @ViewChild('logBody') logBody?: ElementRef<HTMLDivElement>;

  entries: LogEntry[] = [];
  isLoading = true;
  minLevel = 'Information';
  filterText = '';
  follow = true;
  isPaused = false;

  private lastSeq = 0;
  private pollSubscription: Subscription;
  private readonly logsUrl: string;

  constructor(
    private dialogRef: MatDialogRef<LogDialogComponent>,
    private http: HttpClient,
    environmentService: EnvironmentService
  ) {
    this.logsUrl = environmentService.getApiEndpoint('logs');

    // Poll the tail every 2 seconds while open
    this.pollSubscription = timer(0, 2000).subscribe(() => {
      if (!this.isPaused) {
        this.poll();
      }
    });
  }

  ngOnDestroy(): void {
    this.pollSubscription.unsubscribe();
  }

  visibleEntries(): LogEntry[] {
    const minRank = LEVEL_RANK[this.minLevel] ?? 2;
    const filter = this.filterText.trim().toLowerCase();

    return this.entries.filter(e => {
      if ((LEVEL_RANK[e.level] ?? 2) < minRank) return false;
      if (filter && !e.message.toLowerCase().includes(filter) && !(e.exception || '').toLowerCase().includes(filter)) return false;
      return true;
    });
  }

  trackBySeq(_: number, entry: LogEntry): number {
    return entry.seq;
  }

  levelAbbr(level: string): string {
    switch (level) {
      case 'Verbose': return 'VRB';
      case 'Debug': return 'DBG';
      case 'Information': return 'INF';
      case 'Warning': return 'WRN';
      case 'Error': return 'ERR';
      case 'Fatal': return 'FTL';
      default: return level.substring(0, 3).toUpperCase();
    }
  }

  levelClass(level: string): string {
    return 'level-' + this.levelAbbr(level).toLowerCase();
  }

  togglePause(): void {
    this.isPaused = !this.isPaused;
  }

  close(): void {
    this.dialogRef.close();
  }

  private poll(): void {
    this.http.get<LogTailResponse>(this.logsUrl, { params: { after: this.lastSeq } }).subscribe({
      next: response => {
        this.isLoading = false;
        if (response.entries.length > 0) {
          this.entries = [...this.entries, ...response.entries].slice(-1000);
          if (this.follow) {
            setTimeout(() => this.scrollToBottom(), 0);
          }
        }
        this.lastSeq = response.lastSeq;
      },
      error: () => {
        // transient poll failures are fine; the next tick retries
        this.isLoading = false;
      }
    });
  }

  private scrollToBottom(): void {
    const el = this.logBody?.nativeElement;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }
}
