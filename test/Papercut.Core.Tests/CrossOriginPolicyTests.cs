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

    [TestCase(null, new string[0])]
    [TestCase("  ", new string[0])]
    [TestCase("*", new[] { "*" })]
    [TestCase("http://a.example/; https://b.example , http://c.example", new[] { "http://a.example", "https://b.example", "http://c.example" })]
    public void ParseAllowedOrigins_SplitsAndNormalizes(string? value, string[] expected)
    {
        CrossOriginPolicy.ParseAllowedOrigins(value).Should().Equal(expected);
    }
}
