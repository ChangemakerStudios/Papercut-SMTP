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

namespace Papercut.Rules
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using MimeKit;

    using Newtonsoft.Json;

    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Rules;

    [Serializable]
    public abstract class RuleBase : IRule
    {
        bool _isEnabled;

        protected RuleBase()
        {
            Id = Guid.NewGuid();
        }

        [Category("Information")]
        public Guid Id { get; protected set; }

        [Category("State")]
        [Browsable(true)]
        [DisplayName("Is Enabled")]
        [Description("Is the Rule Enabled for Processing?")]
        public virtual bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value.Equals(_isEnabled)) return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        [Category("Information")]
        [Browsable(false)]
        public virtual string Type => GetType().Name;

        [Category("Information")]
        [Browsable(false)]
        [JsonIgnore]
        public virtual string Description
            =>
                GetPropertiesForDescription()
                    .Where(s => !s.Key.IsAny("Id", "Type", "Description"))
                    .OrderBy(s => s.Key)
                    .ToFormattedPairs()
                    .Join("\r\n");

        public abstract void PopulateFromRule(MimeMessage message);

        protected virtual IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
        {
            return this.GetProperties();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                handler(this, new PropertyChangedEventArgs("Description"));
            }
        }
    }
}