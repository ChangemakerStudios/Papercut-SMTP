namespace Papercut.Core.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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

            LoadSettings(JsonHelpers.LoadJson<Dictionary<string, string>>(SettingsFilePath));
        }

        public override void Save()
        {
            if (SettingsFilePath == null) return;

            JsonHelpers.SaveJson(GetSettingSnapshot(), SettingsFilePath);
        }
    }
}