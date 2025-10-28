// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2025 Jaben Cargman
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


using System.Windows.Input;

using ICSharpCode.AvalonEdit.Document;

using Papercut.Helpers;
using Papercut.Views;

namespace Papercut.ViewModels;

public class MessageDetailHeaderViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    string? _headers;

    ZoomIndicator? _zoomIndicator;
    readonly SettingsSaveDebouncer<double> _zoomSaveDebouncer;

    public MessageDetailHeaderViewModel(ILogger logger)
    {
        this._logger = logger;
        this.DisplayName = "Headers";

        // Set up debounced zoom save to reduce I/O during rapid zoom changes
        _zoomSaveDebouncer = new SettingsSaveDebouncer<double>(newFontSize =>
        {
            Settings.Default.TextViewZoomFontSize = newFontSize;
            Settings.Default.Save();
        });
    }

    public string? Headers
    {
        get => this._headers;
        set
        {
            this._headers = value;
            this.NotifyOfPropertyChange(() => this.Headers);
        }
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (view is not MessageDetailHeaderView typedView)
        {
            this._logger.Error("Unable to locate the MessageDetailHeaderView to hook the Text Control");
            return;
        }

        // Store reference to zoom indicator
        _zoomIndicator = typedView.zoomIndicator;

        // Restore saved zoom level
        typedView.HeaderEdit.FontSize = Settings.Default.TextViewZoomFontSize;

        // Install modern search panel for Ctrl+F support
        ModernSearchPanel.Install(typedView.HeaderEdit);

        this.GetPropertyValues(p => p.Headers)
            .Subscribe(
                t => { typedView.HeaderEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });

        // Hook up zoom functionality
        typedView.HeaderEdit.PreviewMouseWheel += (sender, e) =>
        {
            if (ZoomHelper.IsZoomModifierPressed())
            {
                e.Handled = true;
                var newFontSize = ZoomHelper.CalculateNewZoom(
                    typedView.HeaderEdit.FontSize,
                    e.Delta,
                    ZoomHelper.AvalonEditZoom.Increment,
                    ZoomHelper.AvalonEditZoom.MinFontSize,
                    ZoomHelper.AvalonEditZoom.MaxFontSize);
                typedView.HeaderEdit.FontSize = newFontSize;

                // Debounce settings save to reduce I/O during rapid zoom changes
                _zoomSaveDebouncer.OnValueChanged(newFontSize);

                // Show zoom indicator
                _zoomIndicator?.ShowZoomFromFontSize(newFontSize, ZoomHelper.AvalonEditZoom.DefaultFontSize);
            }
        };
    }
}