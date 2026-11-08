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
using Papercut.Core.Domain.Rules;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Rules.Tests;

[TestFixture]
public class RuleServiceBaseFileTests
{
    private string _root = null!;

    private string _legacyFile = null!;

    private string _ruleFile = null!;

    private Mock<IRuleRepository> _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "papercut-rules-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        _legacyFile = Path.Combine(_root, "legacy", "rules.json");
        _ruleFile = Path.Combine(_root, "appdata", "rules.json");

        _repository = new Mock<IRuleRepository>();
        _repository.Setup(r => r.LoadRules(It.IsAny<string>())).Returns(new List<IRule>());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Test]
    public void Load_RuleFileMissing_CopiesLegacyFile()
    {
        WriteFile(_legacyFile, "legacy");

        _ = CreateService().Rules;

        File.ReadAllText(_ruleFile).Should().Be("legacy");
        File.Exists(_legacyFile).Should().BeTrue("the legacy file is copied, not moved");
        _repository.Verify(r => r.LoadRules(_ruleFile), Times.Once);
    }

    [Test]
    public void Load_RuleFileExists_KeepsIt()
    {
        WriteFile(_legacyFile, "legacy");
        WriteFile(_ruleFile, "current");

        _ = CreateService().Rules;

        File.ReadAllText(_ruleFile).Should().Be("current");
    }

    [Test]
    public void Load_NoFiles_CreatesNothing()
    {
        _ = CreateService().Rules;

        File.Exists(_ruleFile).Should().BeFalse();
    }

    [Test]
    public void Save_MissingDirectory_CreatesIt()
    {
        var service = CreateService();

        service.Save();

        Directory.Exists(Path.GetDirectoryName(_ruleFile)).Should().BeTrue();
        _repository.Verify(r => r.SaveRules(It.IsAny<IList<IRule>>(), _ruleFile), Times.Once);
    }

    [Test]
    public void DefaultPaths_UseBaseDirectory()
    {
        var service = new TestRuleService(_repository.Object);

        service.RuleFileName.Should().Be(RuleServiceBase.DefaultRuleFileName);
        service.LegacyRuleFileName.Should().Be(RuleServiceBase.DefaultRuleFileName);
    }

    private TestRuleService CreateService() => new(_repository.Object, _ruleFile, _legacyFile);

    private static void WriteFile(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private class TestRuleService(
        IRuleRepository repository,
        string? ruleFileName = null,
        string? legacyRuleFileName = null)
        : RuleServiceBase(repository, new LoggerConfiguration().CreateLogger(), ruleFileName, legacyRuleFileName);
}
