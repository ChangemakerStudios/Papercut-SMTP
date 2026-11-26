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

using AwesomeAssertions;
using NUnit.Framework;
using Papercut.Core.Domain.Rules;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Cleanup;
using Papercut.Rules.Domain.Forwarding;
using Papercut.Rules.Domain.Invoking;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Rules.Tests;

[TestFixture]
public class RuleUpdatePolicyTests
{
    private static InvokeProcessRule Invoke(string process, string args) =>
        new() { ProcessToRun = process, ProcessCommandLine = args };

    private static ForwardRule Forward(string server, int port = 25, bool ssl = false, string user = "user", string password = "") =>
        new() { SmtpServer = server, SmtpPort = port, SmtpUseSSL = ssl, SmtpUsername = user, SmtpPassword = password };

    #region FindNewInvokeProcessRules

    [Test]
    public void FindNewInvokeProcessRules_NewRule_IsReported()
    {
        var incoming = new IRule[] { Invoke("/bin/sh", "-c \"id\"") };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, []).Should().HaveCount(1);
    }

    [Test]
    public void FindNewInvokeProcessRules_UnchangedStoredRule_IsAllowed()
    {
        var existing = new IRule[] { Invoke(@"C:\tools\notify.exe", "\"%e\"") };
        var incoming = new IRule[] { Invoke(@"C:\tools\notify.exe", "\"%e\""), new MailRetentionRule() };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, existing).Should().BeEmpty();
    }

    [Test]
    public void FindNewInvokeProcessRules_ChangedProcess_IsReported()
    {
        var existing = new IRule[] { Invoke(@"C:\tools\notify.exe", "\"%e\"") };
        var incoming = new IRule[] { Invoke("/bin/sh", "\"%e\"") };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, existing).Should().HaveCount(1);
    }

    [Test]
    public void FindNewInvokeProcessRules_ChangedArguments_IsReported()
    {
        var existing = new IRule[] { Invoke(@"C:\tools\notify.exe", "\"%e\"") };
        var incoming = new IRule[] { Invoke(@"C:\tools\notify.exe", "\"%e\" & calc") };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, existing).Should().HaveCount(1);
    }

    [Test]
    public void FindNewInvokeProcessRules_CaseChange_IsReported()
    {
        var existing = new IRule[] { Invoke("/usr/bin/notify", "%e") };
        var incoming = new IRule[] { Invoke("/usr/bin/NOTIFY", "%e") };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, existing).Should().HaveCount(1);
    }

    [Test]
    public void FindNewInvokeProcessRules_OtherRuleTypes_AreIgnored()
    {
        var incoming = new IRule[] { Forward("smtp.example.com"), new MailRetentionRule() };

        RuleUpdatePolicy.FindNewInvokeProcessRules(incoming, []).Should().BeEmpty();
    }

    #endregion

    #region FindStoredSmtpPassword

    [Test]
    public void FindStoredSmtpPassword_SameConnection_ReturnsStoredPassword()
    {
        var stored = Forward("smtp.example.com", password: "secret");

        RuleUpdatePolicy.FindStoredSmtpPassword(Forward("smtp.example.com"), stored.Id, [stored])
            .Should().Be("secret");
    }

    [Test]
    public void FindStoredSmtpPassword_ServerHostCaseAndWhitespace_StillMatches()
    {
        var stored = Forward("smtp.example.com", password: "secret");

        RuleUpdatePolicy.FindStoredSmtpPassword(Forward(" SMTP.Example.com "), null, [stored])
            .Should().Be("secret");
    }

    [TestCase("attacker.example", 25, false, "user")]
    [TestCase("smtp.example.com", 2525, false, "user")]
    [TestCase("smtp.example.com", 25, true, "user")]
    [TestCase("smtp.example.com", 25, false, "other")]
    public void FindStoredSmtpPassword_ChangedConnection_ReturnsNull(string server, int port, bool ssl, string user)
    {
        var stored = Forward("smtp.example.com", password: "secret");

        RuleUpdatePolicy.FindStoredSmtpPassword(Forward(server, port, ssl, user), stored.Id, [stored])
            .Should().BeNull();
    }

    [Test]
    public void FindStoredSmtpPassword_PrefersRuleWithSameId()
    {
        var first = Forward("smtp.example.com", password: "first");
        var second = Forward("smtp.example.com", password: "second");

        RuleUpdatePolicy.FindStoredSmtpPassword(Forward("smtp.example.com"), second.Id, [first, second])
            .Should().Be("second");
    }

    [Test]
    public void FindStoredSmtpPassword_NoStoredPassword_ReturnsNull()
    {
        var stored = Forward("smtp.example.com");

        RuleUpdatePolicy.FindStoredSmtpPassword(Forward("smtp.example.com"), stored.Id, [stored])
            .Should().BeNull();
    }

    [Test]
    public void FindStoredSmtpPassword_PlainRelayRule_Matches()
    {
        var stored = new RelayRule { SmtpServer = "smtp.example.com", SmtpPort = 25, SmtpUsername = "user", SmtpPassword = "secret" };
        var incoming = new RelayRule { SmtpServer = "smtp.example.com", SmtpPort = 25, SmtpUsername = "user" };

        RuleUpdatePolicy.FindStoredSmtpPassword(incoming, stored.Id, [stored]).Should().Be("secret");
    }

    #endregion

    #region WithoutInvokeProcessRules

    [Test]
    public void WithoutInvokeProcessRules_Disabled_RemovesOnlyInvokeProcessRules()
    {
        var forward = Forward("smtp.example.com");
        var rules = new INewMessageRule[] { Invoke("/bin/sh", "-c id"), forward };

        RuleUpdatePolicy.WithoutInvokeProcessRules(rules, invokeProcessEnabled: false)
            .Should().Equal(forward);
    }

    [Test]
    public void WithoutInvokeProcessRules_Enabled_KeepsAll()
    {
        var rules = new INewMessageRule[] { Invoke("/bin/sh", "-c id"), Forward("smtp.example.com") };

        RuleUpdatePolicy.WithoutInvokeProcessRules(rules, invokeProcessEnabled: true)
            .Should().HaveCount(2);
    }

    #endregion
}
