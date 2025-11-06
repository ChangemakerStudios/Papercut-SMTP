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
using Papercut.Message;

namespace Papercut.Message.Tests;

[TestFixture]
public class MessageRepositoryStaticTests
{
    #region MakeValidFileName Tests

    [Test]
    public void MakeValidFileName_WithValidString_ReturnsUnchanged()
    {
        var result = MessageRepository.MakeValidFileName("simple-filename");
        result.Should().Be("simple-filename");
    }

    [Test]
    public void MakeValidFileName_WithInvalidChars_ReplacesWithUnderscore()
    {
        var result = MessageRepository.MakeValidFileName("file:name*with?invalid|chars", replacement: '_');
        result.Should().NotContain(":");
        result.Should().NotContain("*");
        result.Should().NotContain("?");
        result.Should().NotContain("|");
    }

    [Test]
    public void MakeValidFileName_WithFancyMode_ReplacesQuotesWithUnicode()
    {
        var result = MessageRepository.MakeValidFileName("test\"quoted\"", fancy: true);
        result.Should().Be("test\u201dquoted\u201d");
    }

    [Test]
    public void MakeValidFileName_WithFancyMode_ReplacesSingleQuotesWithUnicode()
    {
        var result = MessageRepository.MakeValidFileName("test'quoted'", fancy: true);
        // Single quote is not an invalid filename char, so it won't be replaced
        result.Should().Be("test'quoted'");
    }

    [Test]
    public void MakeValidFileName_WithFancyMode_ReplacesSlashWithFractionSlash()
    {
        var result = MessageRepository.MakeValidFileName("test/path", fancy: true);
        result.Should().Be("test\u2044path");
    }

    [Test]
    public void MakeValidFileName_WithNullReplacement_RemovesBadChars()
    {
        var result = MessageRepository.MakeValidFileName("test:name", replacement: null);
        result.Should().Be("testname");
    }

    [Test]
    public void MakeValidFileName_WithEmptyString_ReturnsEmptyTextParameter()
    {
        var result = MessageRepository.MakeValidFileName("", emptyText: "unknown");
        result.Should().Be("unknown");
    }

    [Test]
    public void MakeValidFileName_WithNullString_ReturnsEmptyTextParameter()
    {
        var result = MessageRepository.MakeValidFileName(null, emptyText: "default");
        result.Should().Be("default");
    }

    [Test]
    public void MakeValidFileName_WithOnlyInvalidChars_ReturnsEmptyTextParameter()
    {
        var result = MessageRepository.MakeValidFileName(":<>|", emptyText: "fallback", replacement: null);
        result.Should().Be("fallback");
    }

    [Test]
    public void MakeValidFileName_WithInvalidEmptyText_SanitizesEmptyText()
    {
        var result = MessageRepository.MakeValidFileName("", emptyText: "test:name");
        result.Should().NotContain(":");
    }

    [Test]
    public void MakeValidFileName_WithUnicodeCharacters_PreservesValidUnicode()
    {
        var result = MessageRepository.MakeValidFileName("测试文件名");
        result.Should().Be("测试文件名");
    }

    [Test]
    public void MakeValidFileName_WithMixedValidAndInvalid_ReplacesOnlyInvalid()
    {
        var result = MessageRepository.MakeValidFileName("valid<invalid>valid");
        result.Should().Be("valid_invalid_valid");
    }

    #endregion
}
