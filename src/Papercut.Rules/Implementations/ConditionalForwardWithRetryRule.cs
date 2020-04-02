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

namespace Papercut.Rules.Implementations
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;

    [Serializable]
    public class ConditionalForwardWithRetryRule : ConditionalForwardRule
    {
        private int _retryAttemptDelaySeconds;
        private int _retryAttempts;

        public ConditionalForwardWithRetryRule()
        {
            this.RetryAttempts = 5;
            this.RetryAttemptDelaySeconds = 60;
        }

        [Category("Settings")]
        [DisplayName("Retry Attempts")]
        public int RetryAttempts
        {
            get { return _retryAttempts; }
            set
            {
                if (value == _retryAttempts) return;
                _retryAttempts = value;
                OnPropertyChanged(nameof(RetryAttempts));
            }
        }

        [Category("Settings")]
        [DisplayName("Retry Attempt Delay in Seconds")]
        public int RetryAttemptDelaySeconds
        {
            get { return _retryAttemptDelaySeconds; }
            set
            {
                if (value == _retryAttemptDelaySeconds) return;
                _retryAttemptDelaySeconds = value;
                OnPropertyChanged(nameof(RetryAttemptDelaySeconds));
            }
        }

        [Category("Information")]
        public override string Type => "Conditional Forward with Retry";

        public override string ToString()
        {
            return this.GetProperties().OrderBy(s => s.Key).ToFormattedPairs().Join("\r\n");
        }
    }
}