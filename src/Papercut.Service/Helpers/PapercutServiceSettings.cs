// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Service.Helpers
{
    using Papercut.Core.Domain.Settings;

    public class PapercutServiceSettings : ISettingsTyped
    {
        public ISettingStore Settings { get; set; }

        public string IP
        {
            get { return Settings.Get("IP", "Any"); }
            set { if (IP != value) Settings.Set("IP", value); }
        }

        public int Port
        {
            get { return Settings.Get("Port", 25); }
            set { if (Port != value) Settings.Set("Port", value); }
        }

        public string MessagePath
        {
            get { return Settings.Get<string>("MessagePath", @"%BaseDirectory%\Incoming"); }
            set { if (MessagePath != value) Settings.Set("MessagePath", value); }
        }
    }
}