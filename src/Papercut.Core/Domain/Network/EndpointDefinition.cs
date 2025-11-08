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

namespace Papercut.Core.Domain.Network;

public class EndpointDefinition
{
    public EndpointDefinition(string address, int port)
    {
        this.Address = this.ParseIpAddress(address);
        this.Port = port;
    }

    public EndpointDefinition(
        string address,
        int port,
        X509FindType certificateFindType,
        string certificateFindValue,
        StoreLocation storeLocation = StoreLocation.LocalMachine,
        StoreName storeName = StoreName.My)
        : this(address, port)
    {
        this.Certificate = this.LoadCertificateFromStore(
            certificateFindType,
            certificateFindValue,
            storeLocation,
            storeName);
    }

    public IPAddress Address { get; }

    public int Port { get; }

    public X509Certificate? Certificate { get; }

    public IPEndPoint ToIPEndPoint()
    {
        return new IPEndPoint(this.Address, this.Port);
    }

    private IPAddress ParseIpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "any", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.Any;
        }

        return IPAddress.Parse(value);
    }

    private X509Certificate? LoadCertificateFromStore(
        X509FindType findType,
        string findValue,
        StoreLocation storeLocation,
        StoreName storeName)
    {
        using var store = new X509Store(storeName, storeLocation);

        try
        {
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            // Normalize thumbprint search values (remove whitespace, uppercase)
            var normalizedFindValue = findValue;
            if (findType == X509FindType.FindByThumbprint)
            {
                normalizedFindValue = findValue.Replace(" ", "").Replace(":", "").ToUpperInvariant();
            }

            var certificates = store.Certificates.Find(findType, normalizedFindValue, validOnly: false);

            if (certificates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No certificate found matching {findType}='{normalizedFindValue}' in {storeLocation}\\{storeName} store.");
            }

            if (certificates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple certificates ({certificates.Count}) found matching {findType}='{normalizedFindValue}' in {storeLocation}\\{storeName} store. Please provide a more specific search criteria.");
            }

            var certificate = certificates[0];

            // Validate certificate has private key (required for TLS server)
            if (certificate is X509Certificate2 cert2)
            {
                if (!cert2.HasPrivateKey)
                {
                    throw new InvalidOperationException(
                        $"Certificate '{cert2.Subject}' does not have a private key. TLS/STARTTLS requires a certificate with a private key.");
                }

                // Log warnings for certificate validity issues
                var now = DateTime.Now;
                if (cert2.NotBefore > now)
                {
                    Log.Warning(
                        "Certificate '{Subject}' is not yet valid (NotBefore: {NotBefore}, Current: {Now})",
                        cert2.Subject,
                        cert2.NotBefore,
                        now);
                }
                else if (cert2.NotAfter < now)
                {
                    Log.Warning(
                        "Certificate '{Subject}' has expired (NotAfter: {NotAfter}, Current: {Now})",
                        cert2.Subject,
                        cert2.NotAfter,
                        now);
                }
            }

            return certificate;
        }
        finally
        {
            store.Close();
        }
    }

    public override string ToString()
    {
        var certInfo = this.Certificate != null ? " (with TLS)" : string.Empty;
        return $"{this.Address}:{this.Port}{certInfo}";
    }
}