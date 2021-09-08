// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Helpers
{
    using System;

    using Microsoft.Web.WebView2.Core;

    using Papercut.Core.Annotations;

    public static class DisableEdgeFeaturesHelper
    {
        public static void DisableEdgeFeatures([NotNull] this CoreWebView2 coreWeb)
        {
            if (coreWeb == null) throw new ArgumentNullException(nameof(coreWeb));

            coreWeb.Settings.AreDefaultContextMenusEnabled = false;
            coreWeb.Settings.IsZoomControlEnabled = false;
            coreWeb.Settings.AreDevToolsEnabled = false;
            coreWeb.Settings.AreDefaultScriptDialogsEnabled = false;
            coreWeb.Settings.IsBuiltInErrorPageEnabled = false;

            // Issue #145 fixed
            coreWeb.Settings.IsStatusBarEnabled = true;
        }
    }
}