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
using Papercut.Message;

namespace Papercut.Message.Tests;

[TestFixture]
public class PublishedMessageTrackerTests
{
    private PublishedMessageTracker _tracker = null!;

    private string _messagePath = null!;

    [SetUp]
    public void SetUp()
    {
        _tracker = new PublishedMessageTracker();
        _messagePath = Path.Combine(Path.GetTempPath(), "Incoming", "20260926120000000 Subject abc123.eml");
    }

    [Test]
    public void ClaimAlreadyPublished_AfterMarkPublished_ReturnsTrue()
    {
        _tracker.MarkPublished(_messagePath);

        _tracker.ClaimAlreadyPublished(_messagePath).Should().BeTrue();
    }

    [Test]
    public void ClaimAlreadyPublished_ForUnknownFile_ReturnsFalse()
    {
        // a file dropped straight into the folder was never published by the
        // receiving path, so the watcher must still announce it
        _tracker.ClaimAlreadyPublished(_messagePath).Should().BeFalse();
    }

    [Test]
    public void ClaimAlreadyPublished_ConsumesTheClaim()
    {
        _tracker.MarkPublished(_messagePath);

        _tracker.ClaimAlreadyPublished(_messagePath).Should().BeTrue();
        _tracker.ClaimAlreadyPublished(_messagePath).Should().BeFalse();
    }

    [Test]
    public void ClaimAlreadyPublished_MatchesEquivalentPathSpellings()
    {
        _tracker.MarkPublished(_messagePath);

        var roundabout = Path.Combine(Path.GetDirectoryName(_messagePath)!, ".", Path.GetFileName(_messagePath));

        _tracker.ClaimAlreadyPublished(roundabout).Should().BeTrue();
    }

    [Test]
    public void ClaimAlreadyPublished_CaseHandlingFollowsTheFileSystem()
    {
        _tracker.MarkPublished(_messagePath);

        var differentCase = _messagePath.ToUpperInvariant();

        // same file on Windows and macOS, a different file on Linux
        var expected = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        _tracker.ClaimAlreadyPublished(differentCase).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BlankPaths_AreIgnored(string? path)
    {
        _tracker.MarkPublished(path!);

        _tracker.ClaimAlreadyPublished(path!).Should().BeFalse();
    }

    [Test]
    public void MarkPublished_IsSafeUnderConcurrency()
    {
        var paths = Enumerable.Range(0, 500)
            .Select(i => Path.Combine(Path.GetTempPath(), "Incoming", $"{i}.eml"))
            .ToList();

        Parallel.ForEach(paths, p => _tracker.MarkPublished(p));

        paths.Count(p => _tracker.ClaimAlreadyPublished(p)).Should().Be(paths.Count);
    }
}
