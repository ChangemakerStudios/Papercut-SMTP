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


using System.Windows.Forms;

using Papercut.Domain.UiCommands.Commands;

namespace Papercut.Domain.UiCommands;

public interface IUiCommandHub
{
    IObservable<ShowBalloonTipCommand> OnShowBalloonTip { get; }

    IObservable<ShowOptionWindowCommand> OnShowOptionWindow { get; }

    IObservable<ShowMessageCommand> OnShowMessage { get; }

    IObservable<ShowMainWindowCommand> OnShowMainWindow { get; }

    void ShowMainWindow(bool selectMostRecentMessage = false);

    void ShowMessage(string messageText, string caption);

    void ShowOptionWindow();

    void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon toolTipIcon);
}