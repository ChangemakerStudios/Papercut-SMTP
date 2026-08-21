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

using Papercut.Infrastructure.Smtp.RateLimiting;

using SmtpServer.Protocol;

namespace Papercut.Infrastructure.Smtp.Tests.RateLimiting;

[TestFixture]
public class SmtpRateLimitTests
{
    #region Factory Method Tests

    [Test]
    public void Create_WithValidSpec_ReturnsSuccess()
    {
        // Arrange
        var spec = "500/1h";

        // Act
        var result = SmtpRateLimit.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxMessages.Should().Be(500);
        result.Value.Window.Should().Be(TimeSpan.FromHours(1));
        result.Value.IsUnlimited.Should().BeFalse();
    }

    [TestCase("100/30s", 100, 30)]
    [TestCase("5/10m", 5, 600)]
    [TestCase("500/2h", 500, 7200)]
    public void Create_WithEachWindowUnit_ParsesWindow(string spec, int expectedCount, int expectedSeconds)
    {
        // Act
        var result = SmtpRateLimit.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxMessages.Should().Be(expectedCount);
        result.Value.Window.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Test]
    public void Create_IsCaseInsensitiveOnWindowUnit()
    {
        // Act
        var result = SmtpRateLimit.Create("10/1H");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Window.Should().Be(TimeSpan.FromHours(1));
    }

    [Test]
    public void Create_IgnoresSurroundingWhitespace()
    {
        // Act
        var result = SmtpRateLimit.Create("  500 / 1h  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxMessages.Should().Be(500);
        result.Value.Window.Should().Be(TimeSpan.FromHours(1));
    }

    [TestCase("*")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithNoLimitSpec_ReturnsUnlimited(string? spec)
    {
        // Act
        var result = SmtpRateLimit.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUnlimited.Should().BeTrue();
    }

    [TestCase("500")]
    [TestCase("500/")]
    [TestCase("/1h")]
    [TestCase("500/1h/2")]
    [TestCase("abc/1h")]
    [TestCase("0/1h")]
    [TestCase("-5/1h")]
    [TestCase("500/1d")]
    [TestCase("500/0m")]
    [TestCase("500/h")]
    [TestCase("500/-1m")]
    public void Create_WithInvalidSpec_ReturnsFailureWithError(string spec)
    {
        // Act
        var result = SmtpRateLimit.Create(spec);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region Reply Code Tests

    [Test]
    public void Create_WithoutReplyCode_DefaultsTo451()
    {
        // Act
        var result = SmtpRateLimit.Create("500/1h");

        // Assert
        result.Value.ReplyCode.Should().Be(SmtpReplyCode.Aborted);
        ((int)result.Value.ReplyCode).Should().Be(451);
    }

    [TestCase(421)]
    [TestCase(451)]
    [TestCase(452)]
    [TestCase(550)]
    public void Create_WithSupportedReplyCode_ReturnsSuccess(int replyCode)
    {
        // Act
        var result = SmtpRateLimit.Create("500/1h", replyCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ((int)result.Value.ReplyCode).Should().Be(replyCode);
    }

    [TestCase(250)]
    [TestCase(399)]
    [TestCase(600)]
    [TestCase(0)]
    public void Create_WithNonFailureReplyCode_ReturnsFailure(int replyCode)
    {
        // Act
        var result = SmtpRateLimit.Create("500/1h", replyCode);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Test]
    public void ReplyMessage_ForTransientCode_UsesTransientEnhancedStatus()
    {
        // Act
        var limit = SmtpRateLimit.Create("500/1h", 451).Value;

        // Assert
        limit.ReplyMessage.Should().StartWith("4.7.1");
        limit.ReplyMessage.Should().Contain("500 per hour");
    }

    [Test]
    public void ReplyMessage_ForPermanentCode_UsesPermanentEnhancedStatus()
    {
        // Act
        var limit = SmtpRateLimit.Create("5/10m", 550).Value;

        // Assert
        limit.ReplyMessage.Should().StartWith("5.7.1");
        limit.ReplyMessage.Should().Contain("5 per 10 minutes");
    }

    #endregion
}
