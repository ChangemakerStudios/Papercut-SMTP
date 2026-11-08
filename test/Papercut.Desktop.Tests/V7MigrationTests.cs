// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using AwesomeAssertions;

using NUnit.Framework;

namespace Papercut.Desktop.Tests;

[TestFixture]
public class V7MigrationTests
{
    [Test]
    public void Asks_to_recreate_rules_only_when_updating_from_v7_without_a_copy()
    {
        var rulesFile = Path.Combine(Path.GetTempPath(), $"rules-{Guid.NewGuid():N}.json");

        V7Migration.RulesNeedRecreating(foundV7Settings: true, rulesFile).Should().BeTrue();
        V7Migration.RulesNeedRecreating(foundV7Settings: false, rulesFile).Should().BeFalse();

        File.WriteAllText(rulesFile, "[]");

        try
        {
            V7Migration.RulesNeedRecreating(foundV7Settings: true, rulesFile).Should().BeFalse();
        }
        finally
        {
            File.Delete(rulesFile);
        }
    }

    [Test]
    public void Reads_settings_from_a_wpf_user_config()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-{Guid.NewGuid():N}.config");

        File.WriteAllText(
            path,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                <userSettings>
                    <Papercut.Properties.Settings>
                        <setting name="IP" serializeAs="String">
                            <value>127.0.0.1</value>
                        </setting>
                        <setting name="Port" serializeAs="String">
                            <value>2526</value>
                        </setting>
                        <setting name="MessagePaths" serializeAs="String">
                            <value>%ApplicationData%\Papercut;D:\Mail</value>
                        </setting>
                        <setting name="ShowNotifications" serializeAs="String">
                            <value>False</value>
                        </setting>
                    </Papercut.Properties.Settings>
                </userSettings>
            </configuration>
            """);

        try
        {
            var settings = V7Migration.ReadSettings(path);

            settings.Should().Contain("IP", "127.0.0.1")
                .And.Contain("Port", "2526")
                .And.Contain("MessagePaths", @"%ApplicationData%\Papercut;D:\Mail")
                .And.Contain("ShowNotifications", "False");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
