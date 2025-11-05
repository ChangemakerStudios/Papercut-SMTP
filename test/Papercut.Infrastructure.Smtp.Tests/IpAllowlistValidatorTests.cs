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
public class IpAllowlistValidatorTests
{
    #region Allow All Tests

    [Test]
    public void AllowAll_WithAsterisk_AllowsAnyIPv4()
    {
        // Arrange
        var validator = new IpAllowlistValidator("*");
        var testIp = IPAddress.Parse("203.0.113.45");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AllowAll_WithAsterisk_AllowsAnyIPv6()
    {
        // Arrange
        var validator = new IpAllowlistValidator("*");
        var testIp = IPAddress.Parse("2001:db8::1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AllowAll_WithEmptyString_AllowsAnyIP()
    {
        // Arrange
        var validator = new IpAllowlistValidator("");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AllowAll_WithWhitespace_AllowsAnyIP()
    {
        // Arrange
        var validator = new IpAllowlistValidator("   ");
        var testIp = IPAddress.Parse("10.0.0.1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Localhost Tests

    [Test]
    public void Localhost_IPv4Loopback_AlwaysAllowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24"); // Doesn't include 127.0.0.1
        var loopback = IPAddress.Loopback;

        // Act
        var result = validator.IsAllowed(loopback);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Localhost_IPv6Loopback_AlwaysAllowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24"); // Doesn't include ::1
        var loopback = IPAddress.IPv6Loopback;

        // Act
        var result = validator.IsAllowed(loopback);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Single IP Address Tests

    [Test]
    public void SingleIP_ExactMatch_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void SingleIP_DifferentIP_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100");
        var testIp = IPAddress.Parse("192.168.1.101");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void SingleIP_IPv6ExactMatch_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("2001:db8::1");
        var testIp = IPAddress.Parse("2001:db8::1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Multiple IPs Tests

    [Test]
    public void MultipleIPs_FirstIP_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,192.168.1.101,192.168.1.102");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void MultipleIPs_LastIP_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,192.168.1.101,192.168.1.102");
        var testIp = IPAddress.Parse("192.168.1.102");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void MultipleIPs_NotInList_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,192.168.1.101,192.168.1.102");
        var testIp = IPAddress.Parse("192.168.1.103");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CIDR Range Tests - Basic

    [Test]
    public void CIDR_Slash24_IPInRange_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24");
        var testIp = IPAddress.Parse("192.168.1.150");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash24_IPOutsideRange_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24");
        var testIp = IPAddress.Parse("192.168.2.150");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void CIDR_Slash16_IPInRange_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("10.0.0.0/16");
        var testIp = IPAddress.Parse("10.0.255.255");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash8_IPInRange_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("10.0.0.0/8");
        var testIp = IPAddress.Parse("10.255.255.255");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CIDR Range Tests - Multi-Octet Boundary Cases

    [Test]
    public void CIDR_Slash23_FirstAddress_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse("192.168.0.0");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash23_LastAddress_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse("192.168.1.255");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash23_MiddleAddress_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse("192.168.0.250");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash23_CrossingOctetBoundary_Allowed()
    {
        // Arrange
        // This is the critical test case for the bug fix
        // Range: 192.168.0.0 to 192.168.1.255
        // Testing: 192.168.0.250 (should be allowed)
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse("192.168.0.250");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash23_JustOutsideRange_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse("192.168.2.0");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void CIDR_Slash17_CrossingOctetBoundary_Allowed()
    {
        // Arrange
        // Range: 10.0.0.0 to 10.0.127.255
        var validator = new IpAllowlistValidator("10.0.0.0/17");
        var testIp = IPAddress.Parse("10.0.100.250");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CIDR Range Tests - Edge Cases

    [Test]
    public void CIDR_Slash32_SingleIP_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100/32");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_Slash32_DifferentIP_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100/32");
        var testIp = IPAddress.Parse("192.168.1.101");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void CIDR_Slash0_AllIPsAllowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("0.0.0.0/0");
        var testIp = IPAddress.Parse("203.0.113.45");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IPv6 CIDR Tests

    [Test]
    public void CIDR_IPv6_Slash64_InRange_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("2001:db8::/64");
        var testIp = IPAddress.Parse("2001:db8::1234");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void CIDR_IPv6_Slash64_OutsideRange_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("2001:db8::/64");
        var testIp = IPAddress.Parse("2001:db8:0:1::1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void CIDR_IPv6_Slash128_ExactMatch_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("2001:db8::1/128");
        var testIp = IPAddress.Parse("2001:db8::1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Mixed Configuration Tests

    [Test]
    public void Mixed_IPsAndCIDRs_MatchesSingleIP_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,10.0.0.0/8,172.16.0.1");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Mixed_IPsAndCIDRs_MatchesCIDR_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,10.0.0.0/8,172.16.0.1");
        var testIp = IPAddress.Parse("10.50.100.200");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Mixed_IPsAndCIDRs_NoMatch_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,10.0.0.0/8,172.16.0.1");
        var testIp = IPAddress.Parse("192.168.1.101");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Mixed_WithAsterisk_AllowsAll()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100,*,10.0.0.0/8");
        var testIp = IPAddress.Parse("203.0.113.45");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IPv4-Mapped IPv6 Tests

    [Test]
    public void IPv4MappedToIPv6_MatchesIPv4CIDR_Allowed()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24");
        var ipv4 = IPAddress.Parse("192.168.1.100");
        var ipv4MappedToIPv6 = ipv4.MapToIPv6();

        // Act
        var result = validator.IsAllowed(ipv4MappedToIPv6);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Address Family Mismatch Tests

    [Test]
    public void AddressFamilyMismatch_IPv4AddressIPv6Range_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("2001:db8::/64");
        var testIp = IPAddress.Parse("192.168.1.100");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void AddressFamilyMismatch_IPv6AddressIPv4Range_Rejected()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.0/24");
        var testIp = IPAddress.Parse("2001:db8::1");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void InvalidCIDR_InvalidIPAddress_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            new IpAllowlistValidator("not.an.ip.address/24");
        });

        ex!.Message.Should().Contain("Invalid IP address in CIDR");
    }

    [Test]
    public void InvalidCIDR_InvalidPrefixLength_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            new IpAllowlistValidator("192.168.1.0/abc");
        });

        ex!.Message.Should().Contain("Invalid prefix length in CIDR");
    }

    [Test]
    public void InvalidCIDR_PrefixTooLarge_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            new IpAllowlistValidator("192.168.1.0/33");
        });

        ex!.Message.Should().Contain("Prefix length must be between 0 and 32");
    }

    [Test]
    public void InvalidCIDR_NegativePrefix_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            new IpAllowlistValidator("192.168.1.0/-1");
        });

        ex!.Message.Should().Contain("Prefix length must be between 0 and 32");
    }

    [Test]
    public void InvalidCIDR_MissingSlash_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            new IpAllowlistValidator("192.168.1.0-24");
        });

        ex!.Message.Should().Contain("Invalid CIDR notation");
    }

    #endregion

    #region Whitespace Handling Tests

    [Test]
    public void Whitespace_InCommaSeparatedList_HandledCorrectly()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.1.100 , 192.168.1.101 , 192.168.1.102");
        var testIp = IPAddress.Parse("192.168.1.101");

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Real-World Scenario Tests

    [TestCase("192.168.0.1", true)]
    [TestCase("192.168.0.100", true)]
    [TestCase("192.168.0.200", true)]
    [TestCase("192.168.0.250", true)]  // Critical: crosses into next octet range
    [TestCase("192.168.1.0", true)]
    [TestCase("192.168.1.10", true)]
    [TestCase("192.168.1.100", true)]
    [TestCase("192.168.1.200", true)]
    [TestCase("192.168.1.255", true)]
    [TestCase("192.168.2.0", false)]   // Just outside range
    [TestCase("192.167.255.255", false)] // Just below range
    public void RealWorld_Slash23Range_VariousAddresses(string ipString, bool expectedAllowed)
    {
        // Arrange
        // Range: 192.168.0.0 to 192.168.1.255 (512 addresses)
        var validator = new IpAllowlistValidator("192.168.0.0/23");
        var testIp = IPAddress.Parse(ipString);

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().Be(expectedAllowed, $"IP {ipString} should be {(expectedAllowed ? "allowed" : "rejected")}");
    }

    [TestCase("10.0.0.0", true)]
    [TestCase("10.0.127.255", true)]  // Last IP in range
    [TestCase("10.0.128.0", false)]   // First IP outside range
    [TestCase("10.1.0.0", false)]
    public void RealWorld_Slash17Range_BoundaryTests(string ipString, bool expectedAllowed)
    {
        // Arrange
        // Range: 10.0.0.0 to 10.0.127.255 (32768 addresses)
        var validator = new IpAllowlistValidator("10.0.0.0/17");
        var testIp = IPAddress.Parse(ipString);

        // Act
        var result = validator.IsAllowed(testIp);

        // Assert
        result.Should().Be(expectedAllowed, $"IP {ipString} should be {(expectedAllowed ? "allowed" : "rejected")}");
    }

    [Test]
    public void RealWorld_CommonPrivateNetworks_MultipleRanges()
    {
        // Arrange
        var validator = new IpAllowlistValidator("192.168.0.0/16,10.0.0.0/8,172.16.0.0/12");

        // Act & Assert
        validator.IsAllowed(IPAddress.Parse("192.168.1.100")).Should().BeTrue();
        validator.IsAllowed(IPAddress.Parse("10.50.100.200")).Should().BeTrue();
        validator.IsAllowed(IPAddress.Parse("172.20.30.40")).Should().BeTrue();
        validator.IsAllowed(IPAddress.Parse("203.0.113.45")).Should().BeFalse();
    }

    #endregion
}
