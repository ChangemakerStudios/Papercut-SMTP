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


using JetBrains.Annotations;

namespace Papercut.Rules;

[Serializable]
public abstract class RuleBase : IRule
{
    bool _isEnabled;

    protected RuleBase()
    {
        this.Id = Guid.NewGuid();
    }

    [Category("Information")]
    public Guid Id { get; protected set; }

    [Category("State")]
    [Browsable(true)]
    [DisplayName("Is Enabled")]
    [Description("Is the Rule Enabled for Processing?")]
    public virtual bool IsEnabled
    {
        get { return this._isEnabled; }
        set
        {
            if (value.Equals(this._isEnabled)) return;
            this._isEnabled = value;
            this.OnPropertyChanged(nameof(this.IsEnabled));
        }
    }

    [Category("Information")]
    [Browsable(false)]
    public virtual string Type => this.GetType().Name;

    [Category("Information")]
    [Browsable(false)]
    [JsonIgnore]
    public virtual string Description
        =>
            this.GetPropertiesForDescription()
                .Where(s => !new[] { "Id", "Type", "Description" }.Contains(s.Key))
                .OrderBy(s => s.Key)
                .ToFormattedPairs()
                .Join("\r\n");

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
    {
        return this.GetProperties();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler handler = this.PropertyChanged;
        if (handler != null)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
            handler(this, new PropertyChangedEventArgs("Description"));
        }
    }
}