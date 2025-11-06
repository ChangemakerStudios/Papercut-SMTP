// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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

namespace Papercut.Rules.Domain.Invoking;

[Serializable]
public class InvokeProcessRule : NewMessageRuleBase
{
    private string? _processToRun;
    private string? _processCommandLine = @"""%e""";

    [Category("Information")]
    public override string Type => "Invoke Process";

    [Category("Settings")]
    [DisplayName("Process to Run")]
    [Description("Full Path and EXE of Process to Run on New Message")]
    public string? ProcessToRun
    {
        get => _processToRun;
        set
        {
            if (value == _processToRun) return;
            _processToRun = value;
            OnPropertyChanged(nameof(ProcessToRun));
        }
    }

    [Category("Settings")]
    [DisplayName("Process Command Line")]
    [Description(@"Command line to use when running process. Note ""%e"" will be replaced with the full path to the email that triggered the rule.")]
    public string? ProcessCommandLine
    {
        get => this._processCommandLine;
        set
        {
            if (value == this._processCommandLine) return;
            this._processCommandLine = value;
            this.OnPropertyChanged(nameof(this.ProcessCommandLine));
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

        builder.RegisterType<InvokeProcessRule>().AsSelf().As<IRule>().InstancePerDependency();
    }

    #endregion
}