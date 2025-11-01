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


using System.Windows.Input;

using ICSharpCode.AvalonEdit.Document;

using Papercut.Domain.Events;
using Papercut.Helpers;
using Papercut.Views;

namespace Papercut.ViewModels;

public sealed class MessageDetailBodyViewModel : Screen,
    IMessageDetailItem,
    IEventHandler<ThemeChangedEvent>
{
    readonly ILogger _logger;

    string? _body;
    ZoomIndicator? _zoomIndicator;
    MessageDetailBodyView? _view;
    readonly SettingsSaveDebouncer<double> _zoomSaveDebouncer;

    public MessageDetailBodyViewModel(ILogger logger)
    {
        this._logger = logger;
        this.DisplayName = "Body";

        // Set up debounced zoom save to reduce I/O during rapid zoom changes
        _zoomSaveDebouncer = new SettingsSaveDebouncer<double>(newFontSize =>
        {
            Settings.Default.TextViewZoomFontSize = newFontSize;
            Settings.Default.Save();
        });
    }

    public Task HandleAsync(ThemeChangedEvent @event, CancellationToken token)
    {
        if (_view != null)
        {
            AvalonEditThemeHelper.ApplyTheme(_view.BodyEdit);
        }

        return Task.CompletedTask;
    }

    public string? Body
    {
        get => this._body;
        set
        {
            this._body = value;
            this.NotifyOfPropertyChange(() => this.Body);
        }
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (!(view is MessageDetailBodyView typedView))
        {
            this._logger.Error("Unable to locate the MessageDetailBodyView to hook the Text Control");
            return;
        }

        // Store references
        _view = typedView;
        _zoomIndicator = typedView.zoomIndicator;

        // Apply theme colors
        AvalonEditThemeHelper.ApplyTheme(typedView.BodyEdit);

        // Restore saved zoom level
        typedView.BodyEdit.FontSize = Settings.Default.TextViewZoomFontSize;

        // Install modern search panel for Ctrl+F support
        ModernSearchPanel.Install(typedView.BodyEdit);

        this.GetPropertyValues(p => p.Body)
            .Subscribe(
                t => { typedView.BodyEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });

        // Hook up zoom functionality
        typedView.BodyEdit.PreviewMouseWheel += (sender, e) =>
        {
            if (ZoomHelper.IsZoomModifierPressed())
            {
                e.Handled = true;
                var newFontSize = ZoomHelper.CalculateNewZoom(
                    typedView.BodyEdit.FontSize,
                    e.Delta,
                    ZoomHelper.AvalonEditZoom.Increment,
                    ZoomHelper.AvalonEditZoom.MinFontSize,
                    ZoomHelper.AvalonEditZoom.MaxFontSize);
                typedView.BodyEdit.FontSize = newFontSize;

                // Debounce settings save to reduce I/O during rapid zoom changes
                _zoomSaveDebouncer.OnValueChanged(newFontSize);

                // Show zoom indicator
                _zoomIndicator?.ShowZoomFromFontSize(newFontSize, ZoomHelper.AvalonEditZoom.DefaultFontSize);
            }
        };
    }
}