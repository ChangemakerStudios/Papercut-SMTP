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


using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

using Papercut.Core.Domain.Paths;
using Papercut.Properties;

namespace Papercut.Helpers
{
    public class WebView2Base : WebView2
    {
        public WebView2Base()
        {
            var webViewUserDataFolder = PathTemplateHelper.RenderPathTemplate(Settings.Default.WebView2UserFolder);

            this.CreationProperties = new CoreWebView2CreationProperties()
                { UserDataFolder = webViewUserDataFolder };

            Log.Information("Setting WebView2 User Data Folder: {UserDataFolder}", webViewUserDataFolder);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            this.SetVisible(false);

            base.DestroyWindowCore(hwnd);
        }

        public void SetVisible(bool visible)
        {
            if (typeof(WebView2).GetField(
                        "_coreWebView2Controller",
                        BindingFlags.Instance | BindingFlags.NonPublic)?
                    .GetValue(this) is CoreWebView2Controller controller)
            {
                controller.IsVisible = visible;
            }
        }
    }
}