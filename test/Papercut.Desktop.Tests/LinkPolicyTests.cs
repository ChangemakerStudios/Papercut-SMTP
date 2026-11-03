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
public class LinkPolicyTests
{
    private static readonly Uri Origin = new("http://127.0.0.1:37410/");

    [TestCase("http://127.0.0.1:37410/message/abc")]
    [TestCase("http://127.0.0.1:37410/api/messages/abc/raw")]
    [TestCase("about:blank")]
    [TestCase("about:srcdoc")]
    public void Service_origin_and_about_load_in_the_window(string url)
    {
        LinkPolicy.Decide(new Uri(url), Origin).Should().Be(LinkAction.Allow);
    }

    [TestCase("https://example.com/")]
    [TestCase("http://127.0.0.1:8080/")]
    [TestCase("https://127.0.0.1:37410/")]
    [TestCase("http://localhost:37410/")]
    [TestCase("mailto:someone@example.com")]
    public void Other_web_and_mail_links_leave_the_window(string url)
    {
        LinkPolicy.Decide(new Uri(url), Origin).Should().Be(LinkAction.OpenExternally);
    }

    [TestCase("file:///C:/Windows/System32/calc.exe")]
    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<script>alert(1)</script>")]
    [TestCase("ms-settings:privacy")]
    [TestCase("vbscript:msgbox")]
    public void Everything_else_is_blocked(string url)
    {
        LinkPolicy.Decide(new Uri(url), Origin).Should().Be(LinkAction.Block);
    }

    [Test]
    public void Without_a_service_nothing_http_loads_in_the_window()
    {
        LinkPolicy.Decide(new Uri("http://127.0.0.1:37410/"), null).Should().Be(LinkAction.OpenExternally);
    }
}
