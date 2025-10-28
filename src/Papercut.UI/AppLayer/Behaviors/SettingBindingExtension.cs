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

namespace Papercut.AppLayer.Behaviors;

using System.Windows.Data;

/// <summary>
/// Very useful code from here:
/// http://tomlev2.wordpress.com/2008/11/18/wpf-binding-to-application-settings-using-a-markup-extension/
///
/// Modified to prevent saving window dimensions when minimized (Issue #327)
/// </summary>
public class SettingBindingExtension : Binding
{
    public SettingBindingExtension()
    {
        Initialize();
    }

    public SettingBindingExtension(string path)
        : base(path)
    {
        Initialize();
    }

    private void Initialize()
    {
        Source = Properties.Settings.Default;
        Mode = BindingMode.TwoWay;

        // Use a value converter to prevent saving dimensions when window is minimized
        Converter = WindowDimensionConverter.Instance;
    }
}