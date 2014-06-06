/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Linq;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Core.Helper;

    public class PartsListViewModel : Screen
    {
        public PartsListViewModel()
        {
            Parts = new ObservableCollection<MimePart>();
        }

        MimeMessage _mimeMessage;

        public ObservableCollection<MimePart> Parts { get; private set; }

        public MimeMessage MimeMessage
        {
            get
            {
                return _mimeMessage;
            }
            set
            {
                _mimeMessage = value;
                NotifyOfPropertyChange(() => MimeMessage);

                if (_mimeMessage != null)
                {
                    RefreshParts();
                }
            }
        }

        void RefreshParts()
        {
            Parts.Clear();
            Parts.AddRange(MimeMessage.BodyParts.ToList());
        }
    }
}