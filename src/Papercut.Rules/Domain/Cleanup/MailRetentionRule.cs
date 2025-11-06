// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.ComponentModel;

using Autofac;

using Papercut.Common.Extensions;
using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Rules.Domain.Cleanup;

[Serializable]
public class MailRetentionRule : PeriodicBackgroundRuleBase
{
    private const int MinRetentionDays = 1;
    private const int MaxRetentionDays = 3650; // ~10 years

    private int _mailRetentionDays = 7;

    [Category("Information")]
    public override string Type => "Cleanup Mail";

    [Category("Settings")]
    [DisplayName("Mail Retention in Days")]
    [Description("Clean up emails older than X days.")]
    public int MailRetentionDays
    {
        get => _mailRetentionDays;
        set
        {
            if (value == _mailRetentionDays) return;

            _mailRetentionDays = value;
            OnPropertyChanged(nameof(MailRetentionDays));
        }
    }

    protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
    {
        return base.GetPropertiesForDescription().Concat(this.GetProperties());
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<MailRetentionRule>().AsSelf().As<IRule>().InstancePerDependency();
    }

    #endregion
}