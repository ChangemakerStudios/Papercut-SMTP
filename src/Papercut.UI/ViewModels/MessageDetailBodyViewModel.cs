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

using Papercut.Views;

namespace Papercut.ViewModels;

public sealed class MessageDetailBodyViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    string? _body;

    public MessageDetailBodyViewModel(ILogger logger)
    {
        this._logger = logger;
        this.DisplayName = "Body";
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
            }
        };
    }
}