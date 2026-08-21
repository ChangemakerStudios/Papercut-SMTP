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

namespace Papercut.Infrastructure.Smtp.Tests.RateLimiting;

[TestFixture]
public class SmtpRateLimiterTests
{
    private static SmtpRateLimiter CreateLimiter(string spec, out TestTimeProvider time)
    {
        time = new TestTimeProvider();
        return new SmtpRateLimiter(SmtpRateLimit.Create(spec).Value, time);
    }

    [Test]
    public void TryAcquire_WhenUnlimited_AlwaysAllows()
    {
        // Arrange
        var limiter = CreateLimiter("*", out _);

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            limiter.TryAcquire().IsAllowed.Should().BeTrue();
        }
    }

    [Test]
    public void TryAcquire_UpToTheLimit_Allows()
    {
        // Arrange
        var limiter = CreateLimiter("3/10m", out _);

        // Act & Assert
        limiter.TryAcquire().IsAllowed.Should().BeTrue();
        limiter.TryAcquire().IsAllowed.Should().BeTrue();

        var last = limiter.TryAcquire();
        last.IsAllowed.Should().BeTrue();
        last.Count.Should().Be(3);
    }

    [Test]
    public void TryAcquire_PastTheLimit_Denies()
    {
        // Arrange
        var limiter = CreateLimiter("2/10m", out _);
        limiter.TryAcquire();
        limiter.TryAcquire();

        // Act
        var decision = limiter.TryAcquire();

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.Count.Should().Be(2);
    }

    [Test]
    public void TryAcquire_WhenDenied_ReportsTimeUntilWindowResets()
    {
        // Arrange
        var limiter = CreateLimiter("1/10m", out var time);
        limiter.TryAcquire();
        time.Advance(TimeSpan.FromMinutes(4));

        // Act
        var decision = limiter.TryAcquire();

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.RetryAfter.Should().Be(TimeSpan.FromMinutes(6));
    }

    [Test]
    public void TryAcquire_AfterWindowExpires_AllowsAgain()
    {
        // Arrange
        var limiter = CreateLimiter("2/10m", out var time);
        limiter.TryAcquire();
        limiter.TryAcquire();
        limiter.TryAcquire().IsAllowed.Should().BeFalse();

        // Act
        time.Advance(TimeSpan.FromMinutes(10));
        var decision = limiter.TryAcquire();

        // Assert
        decision.IsAllowed.Should().BeTrue();
        decision.Count.Should().Be(1);
    }

    [Test]
    public void TryAcquire_WindowStartsAtFirstMessage_NotAtConstruction()
    {
        // Arrange
        var limiter = CreateLimiter("1/10m", out var time);

        // Act -- an idle hour before any mail arrives must not consume the window
        time.Advance(TimeSpan.FromHours(1));
        limiter.TryAcquire().IsAllowed.Should().BeTrue();
        time.Advance(TimeSpan.FromMinutes(9));

        // Assert
        limiter.TryAcquire().IsAllowed.Should().BeFalse();
    }

    [Test]
    public void TryAcquire_DeniedAttempts_DoNotExtendTheWindow()
    {
        // Arrange
        var limiter = CreateLimiter("1/10m", out var time);
        limiter.TryAcquire();

        // Act -- hammering while throttled should not push the reset further out
        time.Advance(TimeSpan.FromMinutes(9));
        limiter.TryAcquire().IsAllowed.Should().BeFalse();
        time.Advance(TimeSpan.FromMinutes(1));

        // Assert
        limiter.TryAcquire().IsAllowed.Should().BeTrue();
    }

    [Test]
    public void TryAcquire_UnderConcurrency_NeverExceedsTheLimit()
    {
        // Arrange
        var limiter = CreateLimiter("100/10m", out _);
        var allowed = 0;

        // Act
        Parallel.For(0, 1000, _ =>
        {
            if (limiter.TryAcquire().IsAllowed)
            {
                Interlocked.Increment(ref allowed);
            }
        });

        // Assert
        allowed.Should().Be(100);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => this._now;

        public void Advance(TimeSpan by) => this._now = this._now.Add(by);
    }
}
