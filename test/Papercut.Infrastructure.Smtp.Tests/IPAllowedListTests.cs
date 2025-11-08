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


using System.Net;

using AwesomeAssertions;

using NUnit.Framework;

namespace Papercut.Infrastructure.Smtp.Tests;

[TestFixture]
public class IPAllowedListTests
{
    #region Factory Method Tests

    [Test]
    public void Create_WithValidSpec_ReturnsSuccess()
    {
        // Arrange
        var spec = "192.168.1.0/24,10.0.0.1";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public void Create_WithAsterisk_ReturnsSuccess()
    {
        // Arrange
        var spec = "*";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Create_WithNull_DefaultsToAllowAll()
    {
        // Arrange
        string? spec = null;

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("1.2.3.4")).Should().BeTrue();
    }

    [Test]
    public void Create_WithEmptyString_DefaultsToAllowAll()
    {
        // Arrange
        var spec = "";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("1.2.3.4")).Should().BeTrue();
    }

    [Test]
    public void Create_WithWhitespace_DefaultsToAllowAll()
    {
        // Arrange
        var spec = "   ";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("1.2.3.4")).Should().BeTrue();
    }

    [Test]
    public void Create_WithInvalidSpec_ReturnsFailed()
    {
        // Arrange
        var spec = "not.a.valid.ip/24";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid IP allowlist specification"));
    }

    [Test]
    public void Create_WithInvalidCIDR_ReturnsFailed()
    {
        // Arrange
        var spec = "192.168.1.0/99";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid IP allowlist specification"));
    }

    [Test]
    public void Create_WithMalformedIP_ReturnsFailed()
    {
        // Arrange
        var spec = "192.168.1.256"; // Invalid - octet out of range

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid IP allowlist specification"));
        result.Errors.Should().Contain(e => e.Contains("192.168.1.256"));
    }

    [Test]
    public void Create_WithMalformedIPInList_ReturnsFailed()
    {
        // Arrange
        var spec = "192.168.1.100,192.168.1.999,10.0.0.1"; // Middle entry is invalid

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid IP allowlist specification"));
        result.Errors.Should().Contain(e => e.Contains("192.168.1.999"));
    }

    #endregion

    #region Static Factory Properties

    [Test]
    public void AllowAll_AllowsAnyIP()
    {
        // Arrange
        var allowList = IPAllowedList.AllowAll;
        var testIps = new[]
        {
            IPAddress.Parse("1.2.3.4"),
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("2001:db8::1")
        };

        // Act & Assert
        foreach (var ip in testIps)
        {
            allowList.IsAllowed(ip).Should().BeTrue($"AllowAll should allow {ip}");
        }
    }

    [Test]
    public void LocalhostOnly_AllowsLoopback()
    {
        // Arrange
        var allowList = IPAllowedList.LocalhostOnly;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Loopback).Should().BeTrue();
        allowList.IsAllowed(IPAddress.IPv6Loopback).Should().BeTrue();
    }

    [Test]
    public void LocalhostOnly_RejectsNonLoopback()
    {
        // Arrange
        var allowList = IPAllowedList.LocalhostOnly;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Parse("192.168.1.1")).Should().BeFalse();
        allowList.IsAllowed(IPAddress.Parse("10.0.0.1")).Should().BeFalse();
    }

    #endregion

    #region IsAllowed Tests

    [Test]
    public void IsAllowed_WithSingleIP_MatchesExact()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.1.100");
        var allowList = result.Value!;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("192.168.1.101")).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_WithCIDR_MatchesRange()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.1.0/24");
        var allowList = result.Value!;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Parse("192.168.1.1")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("192.168.1.255")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("192.168.2.1")).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_WithMultipleSpecs_MatchesAny()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.1.100,10.0.0.0/8");
        var allowList = result.Value!;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("10.50.100.200")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("172.16.0.1")).Should().BeFalse();
    }

    [Test]
    public void IsAllowed_AlwaysAllowsLoopback()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.1.0/24");
        var allowList = result.Value!;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Loopback).Should().BeTrue();
        allowList.IsAllowed(IPAddress.IPv6Loopback).Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Integration_CommonPrivateNetworks_WorksCorrectly()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.0.0/16,10.0.0.0/8,172.16.0.0/12");
        var allowList = result.Value!;

        // Act & Assert - Private networks should be allowed
        allowList.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("10.50.100.200")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("172.20.30.40")).Should().BeTrue();

        // Public IP should be rejected
        allowList.IsAllowed(IPAddress.Parse("8.8.8.8")).Should().BeFalse();
    }

    [Test]
    public void Integration_MixedIPv4AndIPv6_WorksCorrectly()
    {
        // Arrange
        var result = IPAllowedList.Create("192.168.1.100,2001:db8::/64");
        var allowList = result.Value!;

        // Act & Assert
        allowList.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("2001:db8::1234")).Should().BeTrue();
        allowList.IsAllowed(IPAddress.Parse("192.168.1.101")).Should().BeFalse();
        allowList.IsAllowed(IPAddress.Parse("2001:db8:1::1")).Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Create_WithTrailingWhitespace_HandledCorrectly()
    {
        // Arrange
        var spec = "192.168.1.0/24  ";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
    }

    [Test]
    public void Create_WithSpacesInList_HandledCorrectly()
    {
        // Arrange
        var spec = "192.168.1.100 , 10.0.0.1 , 172.16.0.0/24";

        // Act
        var result = IPAllowedList.Create(spec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("10.0.0.1")).Should().BeTrue();
        result.Value!.IsAllowed(IPAddress.Parse("172.16.0.50")).Should().BeTrue();
    }

    #endregion
}
