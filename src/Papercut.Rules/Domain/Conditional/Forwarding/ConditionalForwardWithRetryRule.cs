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

namespace Papercut.Rules.Domain.Conditional.Forwarding
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    using Autofac;

    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Rules;

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
            get => this._retryAttempts;
            set
            {
                if (value == this._retryAttempts) return;
                this._retryAttempts = value;
                this.OnPropertyChanged(nameof(this.RetryAttempts));
            }
        }

        [Category("Settings")]
        [DisplayName("Retry Attempt Delay in Seconds")]
        public int RetryAttemptDelaySeconds
        {
            get => this._retryAttemptDelaySeconds;
            set
            {
                if (value == this._retryAttemptDelaySeconds) return;
                this._retryAttemptDelaySeconds = value;
                this.OnPropertyChanged(nameof(this.RetryAttemptDelaySeconds));
            }
        }

        [Category("Information")]
        public override string Type => "Conditional Forward with Retry";

        public override string ToString()
        {
            return this.GetProperties().OrderBy(s => s.Key).ToFormattedPairs().Join("\r\n");
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<ConditionalForwardWithRetryRule>().AsSelf().As<IRule>().InstancePerDependency();
        }

        #endregion
    }
}