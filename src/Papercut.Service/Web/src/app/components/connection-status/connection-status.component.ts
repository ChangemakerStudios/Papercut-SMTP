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

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LucideAngularModule, PlugZap } from 'lucide-angular';
import { Observable, of, delay, switchMap, distinctUntilChanged } from 'rxjs';
import { SignalRService } from '../../services/signalr.service';

/**
 * Shows when the service has gone away -- it stopped, crashed, or is being
 * restarted mid-development, which for this app is a normal thing to do.
 * Without it the UI just quietly stops updating and looks fine.
 */
@Component({
  selector: 'app-connection-status',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, LucideAngularModule],
  template: `
    <span class="conn-offline"
          *ngIf="isOffline$ | async"
          role="status"
          matTooltip="Disconnected from the Papercut service — retrying…">
      <lucide-icon [img]="icons.PlugZap" [size]="15"></lucide-icon>
    </span>
  `,
  styles: [`
    :host { display: contents; }

    .conn-offline {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      border-radius: 6px;
      color: var(--pc-danger);
      cursor: default;
      animation: conn-pulse 1.6s ease-in-out infinite;
    }

    @keyframes conn-pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.35; transform: scale(0.9); }
    }

    /* A pulsing icon is an animation someone may have asked not to see; the
       colour and the tooltip still carry the message without it. */
    @media (prefers-reduced-motion: reduce) {
      .conn-offline { animation: none; }
    }
  `]
})
export class ConnectionStatusComponent {
  protected readonly icons = { PlugZap };

  /**
   * SignalR drops to disconnected during its own reconnect attempts, so a
   * short hold keeps an ordinary blip from flashing an alarm. Coming back is
   * reported immediately -- there is nothing to debounce about good news.
   */
  readonly isOffline$: Observable<boolean>;

  private static readonly GraceMs = 2000;

  constructor(signalRService: SignalRService) {
    this.isOffline$ = signalRService.isConnected$.pipe(
      switchMap(connected =>
        connected ? of(false) : of(true).pipe(delay(ConnectionStatusComponent.GraceMs))
      ),
      distinctUntilChanged()
    );
  }
}
