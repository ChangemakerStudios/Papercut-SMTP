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

import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map, of, tap } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData, ConfirmDialogResult } from '../components/confirm-dialog/confirm-dialog.component';
import { UserSettingsService } from './user-settings.service';

/**
 * Opens the app's confirmation dialog. Emits exactly once: true if confirmed,
 * false for every other way out (Cancel, the close button, Escape, a backdrop
 * click), so callers only have to handle the one truthy case.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmService {

  constructor(private dialog: MatDialog, private userSettings: UserSettingsService) {}

  confirm(data: ConfirmDialogData): Observable<boolean> {
    return this.open(data).pipe(map(result => result.confirmed));
  }

  /**
   * The confirmation for deleting messages. The dialog offers "Never ask for
   * confirmation on delete"; once that is chosen this emits true straight
   * away, until the prompt is turned back on in Options.
   */
  confirmDelete(data: ConfirmDialogData): Observable<boolean> {
    if (!this.userSettings.isDeleteConfirmationEnabled()) return of(true);

    return this.open({ ...data, dontAskAgainLabel: 'Never ask for confirmation on delete' }).pipe(
      tap(result => {
        // only a delete that went ahead counts: ticking the box and then
        // cancelling must not silently switch the prompt off
        if (result.confirmed && result.dontAskAgain) {
          this.userSettings.setDeleteConfirmationEnabled(false);
        }
      }),
      map(result => result.confirmed));
  }

  private open(data: ConfirmDialogData): Observable<ConfirmDialogResult> {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, ConfirmDialogResult>(ConfirmDialogComponent, {
        data,
        autoFocus: true,
        restoreFocus: true
      })
      .afterClosed()
      .pipe(map(result => ({
        confirmed: result?.confirmed === true,
        dontAskAgain: result?.dontAskAgain === true
      })));
  }
}
