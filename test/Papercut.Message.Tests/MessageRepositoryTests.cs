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
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Paths;
using Papercut.Message;
using Serilog;

namespace Papercut.Message.Tests;

[TestFixture]
public class MessageRepositoryTests
{
    private Mock<IPathConfigurator> _mockPathConfigurator = null!;
    private Mock<ILogger> _mockLogger = null!;
    private MessageRepository _repository = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPathConfigurator = new Mock<IPathConfigurator>();
        _mockLogger = new Mock<ILogger>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PapercutTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _mockPathConfigurator.Setup(x => x.DefaultSavePath).Returns(_testDirectory);
        _mockPathConfigurator.Setup(x => x.LoadPaths).Returns(new[] { _testDirectory });

        _repository = new MessageRepository(_mockPathConfigurator.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region GetFullMailFilename Tests

    [Test]
    public void GetFullMailFilename_WithValidSubject_CreatesFilenameWithSubject()
    {
        var result = _repository.GetFullMailFilename("Test Email");

        result.Should().Contain("Test Email");
        result.Should().EndWith(".eml");
        Path.GetDirectoryName(result).Should().Be(_testDirectory);
    }

    [Test]
    public void GetFullMailFilename_WithLongSubject_TruncatesTo40Chars()
    {
        var longSubject = new string('a', 100);
        var result = _repository.GetFullMailFilename(longSubject);

        var filename = Path.GetFileName(result);
        var firstIndex = filename.IndexOf(' ');
        var lastIndex = filename.LastIndexOf(' ');
        var subjectPart = filename.Substring(firstIndex + 1, lastIndex - firstIndex - 1).Trim();
        subjectPart.Length.Should().BeLessThanOrEqualTo(40);
    }

    [Test]
    public void GetFullMailFilename_WithInvalidChars_SanitizesSubject()
    {
        var result = _repository.GetFullMailFilename("Test:Subject|With*Invalid?Chars");

        var filename = Path.GetFileName(result);
        filename.Should().NotContain(":");
        filename.Should().NotContain("|");
        filename.Should().NotContain("*");
        filename.Should().NotContain("?");
    }

    [Test]
    public void GetFullMailFilename_WithEmptySubject_UsesDefault()
    {
        var result = _repository.GetFullMailFilename("");

        result.Should().Contain("subject unknown");
        result.Should().EndWith(".eml");
    }

    [Test]
    public void GetFullMailFilename_CalledTwice_GeneratesUniqueNames()
    {
        var result1 = _repository.GetFullMailFilename("Test");
        var result2 = _repository.GetFullMailFilename("Test");

        result1.Should().NotBe(result2);
    }

    #endregion

    #region SaveMessage Tests

    [Test]
    public async Task SaveMessage_WithValidData_CreatesFile()
    {
        var subject = "Test Save";
        var content = "Test email content"u8.ToArray();

        var filename = await _repository.SaveMessage(subject, async stream =>
        {
            await stream.WriteAsync(content);
        });

        File.Exists(filename).Should().BeTrue();
        var savedContent = await File.ReadAllBytesAsync(filename);
        savedContent.Should().Equal(content);
    }

    [Test]
    public async Task SaveMessage_LogsSuccess()
    {
        var subject = "Test Log";

        await _repository.SaveMessage(subject, async stream =>
        {
            await stream.WriteAsync("test"u8.ToArray());
        });

        _mockLogger.Verify(
            x => x.Information(
                It.Is<string>(s => s.Contains("Successfully Saved")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task SaveMessage_WithException_LogsError()
    {
        var subject = "Test Error";

        await _repository.SaveMessage(subject, stream =>
        {
            throw new InvalidOperationException("Test exception");
        });

        _mockLogger.Verify(
            x => x.Error(
                It.IsAny<Exception>(),
                It.Is<string>(s => s.Contains("Failure saving")),
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region GetMessage Tests

    [Test]
    public async Task GetMessage_WithExistingFile_ReturnsContent()
    {
        var testFile = Path.Combine(_testDirectory, "test.eml");
        var content = "Test content"u8.ToArray();
        await File.WriteAllBytesAsync(testFile, content);

        var result = await _repository.GetMessage(testFile);

        result.Should().Equal(content);
    }

    [Test]
    public async Task GetMessage_WithNonExistentFile_ThrowsIOException()
    {
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.eml");

        var action = async () => await _repository.GetMessage(nonExistentFile);
        await action.Should().ThrowAsync<IOException>();
    }

    #endregion

    #region DeleteMessage Tests

    [Test]
    public void DeleteMessage_WithExistingFile_DeletesAndReturnsTrue()
    {
        var testFile = Path.Combine(_testDirectory, "delete-test.eml");
        File.WriteAllText(testFile, "test");
        var entry = new MessageEntry(testFile);

        var result = _repository.DeleteMessage(entry);

        result.Should().BeTrue();
        File.Exists(testFile).Should().BeFalse();
    }

    [Test]
    public void DeleteMessage_WithNonExistentFile_ReturnsFalse()
    {
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.eml");
        var entry = new MessageEntry(nonExistentFile);

        var result = _repository.DeleteMessage(entry);

        result.Should().BeFalse();
    }

    [Test]
    public void DeleteMessage_WithReadOnlyFile_RemovesAttributeAndDeletes()
    {
        var testFile = Path.Combine(_testDirectory, "readonly.eml");
        File.WriteAllText(testFile, "test");
        File.SetAttributes(testFile, FileAttributes.ReadOnly);
        var entry = new MessageEntry(testFile);

        var result = _repository.DeleteMessage(entry);

        result.Should().BeTrue();
        File.Exists(testFile).Should().BeFalse();
    }

    #endregion

    #region LoadMessages Tests

    [Test]
    public void LoadMessages_WithNoFiles_ReturnsEmpty()
    {
        var result = _repository.LoadMessages();

        result.Should().BeEmpty();
    }

    [Test]
    public void LoadMessages_WithMultipleFiles_ReturnsAllMessages()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "msg1.eml"), "test1");
        File.WriteAllText(Path.Combine(_testDirectory, "msg2.eml"), "test2");
        File.WriteAllText(Path.Combine(_testDirectory, "msg3.eml"), "test3");

        var result = _repository.LoadMessages().ToList();

        result.Should().HaveCount(3);
    }

    [Test]
    public void LoadMessages_IgnoresNonEmlFiles()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "msg1.eml"), "test1");
        File.WriteAllText(Path.Combine(_testDirectory, "other.txt"), "test2");

        var result = _repository.LoadMessages().ToList();

        result.Should().HaveCount(1);
    }

    #endregion
}
