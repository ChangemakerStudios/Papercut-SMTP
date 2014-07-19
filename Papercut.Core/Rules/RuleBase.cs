// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Rules
{
    using System;
    using System.ComponentModel;

    using Papercut.Core.Annotations;

    [Serializable]
    public abstract class RuleBase : IRule
    {
        protected RuleBase()
        {
            Id = Guid.NewGuid();
        }

        [Category("Information")]
        public Guid Id { get; protected set; }

        [Category("Information")]
        [Browsable(false)]
        public virtual string Type
        {
            get { return GetType().Name; }
        }

        [Category("Information")]
        [Browsable(false)]
        public virtual string Description
        {
            get { return ToString(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                handler(this, new PropertyChangedEventArgs("Description"));
            }
        }
    }
}