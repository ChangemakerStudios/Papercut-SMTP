namespace Papercut.Core.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Papercut.Core.Helper;

    public class JsonSettingStore : BaseSettingsStore
    {
        protected JsonSettingStore()
        {
        }

        public JsonSettingStore(string appName)
        {
            SettingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                appName + ".json");
        }

        protected string SettingsFilePath { get; set; }

        public override void Load()
        {
            if (SettingsFilePath == null) return;

            var settings =
                JsonHelpers.LoadJson<Dictionary<string, string>>(SettingsFilePath);

            if (settings != null) CurrentSettings = new ConcurrentDictionary<string, string>(settings);
            else CurrentSettings.Clear();
        }

        public override void Save()
        {
            if (SettingsFilePath == null) return;

            JsonHelpers.SaveJson(GetSettingSnapshot(), SettingsFilePath);
        }
    }
}