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
using System.Security.Cryptography.X509Certificates;

using AwesomeAssertions;

using NUnit.Framework;

using Papercut.Core.Domain.Network;

namespace Papercut.Core.Tests;

[TestFixture]
public class EndpointDefinitionTests
{
    [Test]
    public void Constructor_WithBasicParameters_CreatesEndpoint()
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("127.0.0.1", 25);

        // Assert
        endpoint.Address.Should().Be(IPAddress.Parse("127.0.0.1"));
        endpoint.Port.Should().Be(25);
        endpoint.Certificate.Should().BeNull();
    }

    [Test]
    public void Constructor_WithAny_ParsesCorrectly()
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("Any", 25);

        // Assert
        endpoint.Address.Should().Be(IPAddress.Any);
        endpoint.Port.Should().Be(25);
    }

    [Test]
    public void Constructor_WithEmptyAddress_ParsesAsAny()
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("", 25);

        // Assert
        endpoint.Address.Should().Be(IPAddress.Any);
        endpoint.Port.Should().Be(25);
    }

    [Test]
    public void ToIPEndPoint_ReturnsCorrectEndpoint()
    {
        // Arrange
        var endpoint = new EndpointDefinition("192.168.1.1", 587);

        // Act
        var ipEndpoint = endpoint.ToIPEndPoint();

        // Assert
        ipEndpoint.Address.Should().Be(IPAddress.Parse("192.168.1.1"));
        ipEndpoint.Port.Should().Be(587);
    }

    [Test]
    public void ToString_WithoutCertificate_ReturnsAddressAndPort()
    {
        // Arrange
        var endpoint = new EndpointDefinition("127.0.0.1", 25);

        // Act
        var result = endpoint.ToString();

        // Assert
        result.Should().Be("127.0.0.1:25");
    }

    [Test]
    public void ToString_WithCertificate_IndicatesTLS()
    {
        // This test requires a certificate to be installed in the test environment
        // For now, we'll mark it as inconclusive if no cert is available
        try
        {
            // Attempt to create an endpoint with a certificate
            // Note: This will only work if a certificate exists in the store
            var endpoint = new EndpointDefinition(
                "127.0.0.1",
                587,
                X509FindType.FindBySubjectName,
                "localhost",
                StoreLocation.CurrentUser,
                StoreName.My);

            // Act
            var result = endpoint.ToString();

            // Assert
            result.Should().Contain(" (with TLS)");
            result.Should().Contain("127.0.0.1:587");
            endpoint.Certificate.Should().NotBeNull();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No certificate found")
                                                   || ex.Message.Contains("Multiple certificates"))
        {
            Assert.Inconclusive($"Test requires exactly one certificate with SubjectName 'localhost' in CurrentUser\\My store. {ex.Message}");
        }
    }

    [Test]
    public void Constructor_WithCertificate_NoMatchFound_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            new EndpointDefinition(
                "127.0.0.1",
                587,
                X509FindType.FindByThumbprint,
                "NONEXISTENT_THUMBPRINT_12345678",
                StoreLocation.LocalMachine,
                StoreName.My);
        });

        ex!.Message.Should().Contain("No certificate found");
        ex.Message.Should().Contain("FindByThumbprint");
        ex.Message.Should().Contain("NONEXISTENT_THUMBPRINT_12345678");
    }

    [Test]
    public void Constructor_WithIPv6_ParsesCorrectly()
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("::1", 25);

        // Assert
        endpoint.Address.Should().Be(IPAddress.IPv6Loopback);
        endpoint.Port.Should().Be(25);
    }

    [TestCase(25)]
    [TestCase(587)]
    [TestCase(465)]
    [TestCase(2525)]
    public void Constructor_WithVariousPorts_StoresCorrectly(int port)
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("Any", port);

        // Assert
        endpoint.Port.Should().Be(port);
    }

    [Test]
    public void Certificate_Property_IsNullByDefault()
    {
        // Arrange & Act
        var endpoint = new EndpointDefinition("127.0.0.1", 25);

        // Assert
        endpoint.Certificate.Should().BeNull();
    }
}
