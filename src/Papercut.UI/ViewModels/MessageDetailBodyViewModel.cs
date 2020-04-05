// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.ViewModels
{
    using System;

    using Caliburn.Micro;

    using Helpers;

    using ICSharpCode.AvalonEdit.Document;

    using Serilog;

    using Views;

    public class MessageDetailBodyViewModel : Screen, IMessageDetailItem
    {
        readonly ILogger _logger;
        string _body;

        public MessageDetailBodyViewModel(ILogger logger)
        {
            _logger = logger;
            DisplayName = "Body";
        }

        public string Body
        {
            get => _body;
            set
            {
                _body = value;
                NotifyOfPropertyChange(() => Body);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (!(view is MessageDetailBodyView typedView))
            {
                _logger.Error("Unable to locate the MessageDetailBodyView to hook the Text Control");
                return;
            }

            this.GetPropertyValues(p => p.Body)
                .Subscribe(
                    t => { typedView.BodyEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });
        }
    }
}