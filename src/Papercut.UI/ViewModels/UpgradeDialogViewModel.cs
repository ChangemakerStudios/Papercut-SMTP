// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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

namespace Papercut.UI.ViewModels;

using Caliburn.Micro;

public class UpgradeDialogViewModel : Screen
{
    public UpgradeDialogViewModel(
        string currentVersion,
        string newVersion,
        string? releaseNotesHtml)
    {
        this.CurrentVersion = currentVersion;
        this.NewVersion = newVersion;

        // Use provided HTML or fallback message
        if (string.IsNullOrWhiteSpace(releaseNotesHtml))
        {
            this.ReleaseNotesHtml = @"
                <html>
                <head>
                    <style>
                        body {
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            padding: 20px;
                            background-color: white;
                            color: #666;
                        }
                    </style>
                </head>
                <body>
                    <p>No release notes available.</p>
                </body>
                </html>";
        }
        else
        {
            this.ReleaseNotesHtml = releaseNotesHtml;
        }

        this.UserChoice = UpgradeChoice.None;
    }

    public string CurrentVersion { get; }

    public string NewVersion { get; }

    public string ReleaseNotesHtml { get; }

    public UpgradeChoice UserChoice { get; private set; }

    public void Upgrade()
    {
        this.UserChoice = UpgradeChoice.Upgrade;
        this.TryCloseAsync(true);
    }

    public void Ignore()
    {
        this.UserChoice = UpgradeChoice.Ignore;
        this.TryCloseAsync(false);
    }
}

public enum UpgradeChoice
{
    None,
    Upgrade,
    Ignore
}
