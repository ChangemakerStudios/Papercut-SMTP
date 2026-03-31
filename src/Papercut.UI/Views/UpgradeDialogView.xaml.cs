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

namespace Papercut.UI.Views;

using MahApps.Metro.Controls;
using Papercut.UI.ViewModels;

public partial class UpgradeDialogView : MetroWindow
{
    public UpgradeDialogView()
    {
        this.InitializeComponent();
        this.Loaded += this.OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is UpgradeDialogViewModel viewModel)
        {
            try
            {
                // Ensure WebView2 is initialized
                await this.ReleaseNotesWebView.EnsureCoreWebView2Async();

                // Navigate to the release notes HTML
                this.ReleaseNotesWebView.NavigateToString(viewModel.ReleaseNotesHtml);
            }
            catch (Exception ex)
            {
                // Fallback: show error message in the WebView
                var errorHtml = $@"
                    <html>
                    <body style='font-family: Segoe UI, sans-serif; padding: 20px;'>
                        <h3 style='color: #d32f2f;'>Unable to load release notes</h3>
                        <p>{System.Security.SecurityElement.Escape(ex.Message)}</p>
                    </body>
                    </html>";

                this.ReleaseNotesWebView.NavigateToString(errorHtml);
            }
        }
    }
}
