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


using Caliburn.Micro;

using ICSharpCode.AvalonEdit.Document;

using Papercut.Helpers;
using Papercut.Views;

namespace Papercut.ViewModels;

public class MessageDetailHeaderViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    string? _headers;

    public MessageDetailHeaderViewModel(ILogger logger)
    {
        this._logger = logger;
        this.DisplayName = "Headers";
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

        this.GetPropertyValues(p => p.Headers)
            .Subscribe(
                t => { typedView.HeaderEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });
    }
}