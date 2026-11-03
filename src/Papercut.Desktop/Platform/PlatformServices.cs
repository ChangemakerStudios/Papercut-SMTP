// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using Papercut.Desktop.Platform.MacOS;
using Papercut.Desktop.Services;

namespace Papercut.Desktop.Platform;

internal interface INotifier
{
    /// <summary>
    /// <paramref name="onClick" /> runs on a background thread, where the platform supports clicks.
    /// </summary>
    void Show(string title, string text, Action? onClick);
}

internal interface IRunAtStartup
{
    bool IsEnabled { get; }

    void Set(bool enabled);
}

internal static class PlatformServices
{
    public static INotifier CreateNotifier()
    {
#if WINDOWS
        return new Windows.WindowsToastNotifier();
#else
        return OperatingSystem.IsMacOS() ? new MacNotifier() : new NullNotifier();
#endif
    }

    public static IRunAtStartup CreateRunAtStartup()
    {
#if WINDOWS
        return new Windows.WindowsRunAtStartup();
#else
        return new MacLaunchAgent();
#endif
    }

    public static IWindowsServiceControl? CreateWindowsServiceControl()
    {
#if WINDOWS
        return new Windows.WindowsServiceControl();
#else
        return null;
#endif
    }

    private sealed class NullNotifier : INotifier
    {
        public void Show(string title, string text, Action? onClick)
        {
        }
    }
}
