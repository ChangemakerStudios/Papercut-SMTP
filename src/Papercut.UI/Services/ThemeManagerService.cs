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


namespace Papercut.Services
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using MahApps.Metro;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Properties;

    public class ThemeManagerService : IEventHandler<PapercutClientPreStartEvent>, IEventHandler<SettingsUpdatedEvent>
    {
        public void Handle(PapercutClientPreStartEvent @event)
        {
            SetTheme();
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            if (@event.PreviousSettings.Theme == @event.NewSettings.Theme) return;

            SetTheme();
        }

        private static void SetTheme()
        {
            ThemeManager.ChangeAppStyle(
                Application.Current,
                ThemeManager.GetAccent(Settings.Default.Theme),
                ThemeManager.GetAppTheme("BaseLight"));
        }
    }
}