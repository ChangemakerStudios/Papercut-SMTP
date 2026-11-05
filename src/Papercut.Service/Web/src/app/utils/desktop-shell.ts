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


/**
 * True inside the Papercut desktop shell, which appends "PapercutDesktop/<version>" to the
 * webview's user agent. The shell raises OS notifications itself.
 */
export function isDesktopShell(): boolean {
  return /\bPapercutDesktop\//.test(navigator.userAgent);
}

/**
 * The shell routes notification clicks in place with this event (detail: app path, e.g.
 * "/message/abc") instead of reloading the page.
 */
export const DESKTOP_NAVIGATE_EVENT = 'papercut-navigate';
