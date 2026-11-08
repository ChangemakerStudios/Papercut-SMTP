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

using Papercut.Desktop.Services;

namespace Papercut.Desktop.Tests;

[TestFixture]
public class ServiceUrlsTests
{
    [TestCase("http://0.0.0.0:8080", "http://127.0.0.1:8080/")]
    [TestCase("http://+:8080", "http://127.0.0.1:8080/")]
    [TestCase("http://*:9000", "http://127.0.0.1:9000/")]
    [TestCase("http://[::]:8080", "http://127.0.0.1:8080/")]
    [TestCase("http://localhost:5000;https://localhost:5001", "http://localhost:5000/")]
    [TestCase("https://papercut.example.com/papercut", "https://papercut.example.com/papercut/")]
    [TestCase(" http://127.0.0.1:37410/ ", "http://127.0.0.1:37410/")]
    public void Normalizes_kestrel_urls_to_a_client_base(string urls, string expected)
    {
        ServiceUrls.Normalize(urls).Should().Be(new Uri(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a url")]
    [TestCase("ftp://example.com/")]
    public void Rejects_what_is_not_an_http_base(string? urls)
    {
        ServiceUrls.Normalize(urls).Should().BeNull();
    }
}
