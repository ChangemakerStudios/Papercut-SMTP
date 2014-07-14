namespace Papercut.Core.Settings
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Core.Helper;

    public class JsonSettingStore : BaseSettingsStore
    {
        readonly string _saveFilePath;

        public JsonSettingStore(string saveFilePath)
        {
            _saveFilePath = saveFilePath;
        }

        public override void Load()
        {
            var settings = JsonHelpers.LoadJson<IList<KeyValuePair<string, string>>>(_saveFilePath);

            if (settings != null) CurrentSettings = new ConcurrentDictionary<string, string>(settings);
            else CurrentSettings.Clear();
        }

        public override void Save()
        {
            JsonHelpers.SaveJson<IList<KeyValuePair<string, string>>>(
                CurrentSettings.ToList(),
                _saveFilePath);
        }
    }
}