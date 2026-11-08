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

using Papercut.Core.Domain.Network;

namespace Papercut.Core.Tests;

[TestFixture]
public class CrossOriginPolicyTests
{
    private const string Host = "localhost:8080";

    [TestCase(null, Description = "curl, scripts, MCP and the shell send no Origin")]
    [TestCase("")]
    [TestCase("http://localhost:8080", Description = "the web UI itself")]
    [TestCase("HTTP://LOCALHOST:8080")]
    public void IsAllowed_NoOriginOrSameOrigin_IsAllowed(string? origin)
    {
        CrossOriginPolicy.IsAllowed(origin, Host, null, []).Should().BeTrue();
    }

    [TestCase("https://evil.example")]
    [TestCase("http://localhost:9999", Description = "another app on this machine")]
    [TestCase("http://localhost", Description = "same host, different port")]
    [TestCase("http://localhost:8080.evil.example")]
    [TestCase("null", Description = "sandboxed frames and file: pages")]
    [TestCase("not a url")]
    public void IsAllowed_OtherOrigins_AreBlockedByDefault(string origin)
    {
        CrossOriginPolicy.IsAllowed(origin, Host, null, []).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_BehindAProxyThatRewritesHost_MatchesForwardedHost()
    {
        CrossOriginPolicy.IsAllowed("https://mail.corp.example", "127.0.0.1:8080", "mail.corp.example", []).Should().BeTrue();
    }

    [Test]
    public void IsAllowed_ForwardedHostChain_UsesTheFirstEntry()
    {
        CrossOriginPolicy.IsAllowed("https://mail.corp.example", "127.0.0.1:8080", "mail.corp.example, inner.proxy", []).Should().BeTrue();
        CrossOriginPolicy.IsAllowed("https://inner.proxy", "127.0.0.1:8080", "mail.corp.example, inner.proxy", []).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_ExplicitDefaultPortInHost_StillMatches()
    {
        CrossOriginPolicy.IsAllowed("https://mail.corp.example", "mail.corp.example:443", null, []).Should().BeTrue();
    }

    [Test]
    public void IsAllowed_ListedOrigin_IsAllowedAndOthersAreNot()
    {
        var allowed = CrossOriginPolicy.ParseAllowedOrigins("http://localhost:4200");

        CrossOriginPolicy.IsAllowed("http://localhost:4200", Host, null, allowed).Should().BeTrue();
        CrossOriginPolicy.IsAllowed("https://evil.example", Host, null, allowed).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_Wildcard_AllowsAny()
    {
        var allowed = CrossOriginPolicy.ParseAllowedOrigins("*");

        CrossOriginPolicy.IsAllowed("https://anything.example", Host, null, allowed).Should().BeTrue();
        CrossOriginPolicy.IsAllowed("null", Host, null, allowed).Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("localhost:8080")]
    [TestCase("LOCALHOST:8080")]
    [TestCase("127.0.0.1:37410")]
    [TestCase("192.168.1.20:8080", Description = "LAN by address")]
    [TestCase("[::1]:8080")]
    [TestCase("papercut:8080", Description = "Docker service name")]
    [TestCase("my_papercut:8080")]
    [TestCase("devbox")]
    [TestCase("host.docker.internal:8080")]
    [TestCase("papercut.default.svc.cluster.local:8080")]
    [TestCase("devbox.local")]
    [TestCase("papercut.localhost")]
    [TestCase("papercut.test:8080")]
    [TestCase("devbox.home.arpa")]
    public void IsAllowedHost_NamesThatCannotBeRebound_NeedNoSetting(string? host)
    {
        CrossOriginPolicy.IsAllowedHost(host, []).Should().BeTrue();
    }

    [TestCase("evil.example:37410")]
    [TestCase("mail.corp.example")]
    [TestCase("127.0.0.1.nip.io:8080", Description = "public name that resolves to loopback")]
    [TestCase("localhost.evil.example")]
    [TestCase("evil.notlocal", Description = "suffix must match on a label boundary")]
    [TestCase("localhost:8080.evil.example")]
    [TestCase("evil.example.", Description = "trailing dot is still a public name")]
    [TestCase("not a host")]
    public void IsAllowedHost_PublicNames_AreBlockedByDefault(string host)
    {
        CrossOriginPolicy.IsAllowedHost(host, []).Should().BeFalse();
    }

    [TestCase("mail.corp.example")]
    [TestCase("mail.corp.example:8443", Description = "a proxy may change the port")]
    [TestCase("MAIL.CORP.EXAMPLE")]
    public void IsAllowedHost_ListedOriginName_IsAllowed(string host)
    {
        var allowed = CrossOriginPolicy.ParseAllowedOrigins("https://mail.corp.example");

        CrossOriginPolicy.IsAllowedHost(host, allowed).Should().BeTrue();
    }

    [Test]
    public void IsAllowedHost_OtherNamesStayBlockedWhenOneIsListed()
    {
        var allowed = CrossOriginPolicy.ParseAllowedOrigins("https://mail.corp.example");

        CrossOriginPolicy.IsAllowedHost("evil.example", allowed).Should().BeFalse();
    }

    [Test]
    public void IsAllowedHost_Wildcard_AllowsAny()
    {
        CrossOriginPolicy.IsAllowedHost("evil.example", CrossOriginPolicy.ParseAllowedOrigins("*")).Should().BeTrue();
    }

    [TestCase(null, new string[0])]
    [TestCase("  ", new string[0])]
    [TestCase("*", new[] { "*" })]
    [TestCase("http://a.example/; https://b.example , http://c.example", new[] { "http://a.example", "https://b.example", "http://c.example" })]
    public void ParseAllowedOrigins_SplitsAndNormalizes(string? value, string[] expected)
    {
        CrossOriginPolicy.ParseAllowedOrigins(value).Should().Equal(expected);
    }
}
