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


using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Win32;

using global::Windows.Data.Xml.Dom;
using global::Windows.UI.Notifications;

namespace Papercut.Desktop.Platform.Windows;

/// <summary>
/// WinRT toasts under the v7 AUMID, so the toast identity (and the user's notification settings
/// for it) carry over. Velopack's Start-menu shortcut carries the same AUMID; the registry
/// entry makes toasts work for dev runs and portable installs without one.
/// </summary>
internal sealed class WindowsToastNotifier : INotifier
{
    public const string Aumid = "ChangemakerStudios.PapercutSMTP";

    private const int TitleMaxLength = 63;

    private const int TextMaxLength = 255;

    // a toast's Activated only fires while the object is alive
    private readonly Queue<ToastNotification> _recent = new();

    private readonly ToastNotifier? _notifier;

    public WindowsToastNotifier()
    {
        try
        {
            RegisterAumid();
            SetCurrentProcessExplicitAppUserModelID(Aumid);
            _notifier = ToastNotificationManager.CreateToastNotifier(Aumid);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Windows toast notifications are unavailable");
        }
    }

    public void Show(string title, string text, Action? onClick)
    {
        if (_notifier == null) return;

        try
        {
            var xml = new XmlDocument();
            xml.LoadXml(
                $"""
                 <toast>
                   <visual>
                     <binding template="ToastGeneric">
                       <text>{Escape(title, TitleMaxLength)}</text>
                       <text>{Escape(text, TextMaxLength)}</text>
                     </binding>
                   </visual>
                 </toast>
                 """);

            var toast = new ToastNotification(xml);

            if (onClick != null) toast.Activated += (_, _) => onClick();

            lock (_recent)
            {
                _recent.Enqueue(toast);
                while (_recent.Count > 20) _recent.Dequeue();
            }

            _notifier.Show(toast);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show a toast");
        }
    }

    private static string Escape(string value, int maxLength)
    {
        value = value.Trim();

        if (value.Length > maxLength) value = value[..(maxLength - 1)] + "…";

        return SecurityElement.Escape(value);
    }

    private static void RegisterAumid()
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{Aumid}");

        key.SetValue("DisplayName", "Papercut SMTP");

        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "papercut.ico");
        if (File.Exists(icon)) key.SetValue("IconUri", icon);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(string appId);
}
