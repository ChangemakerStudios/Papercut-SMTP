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

            var certificates = store.Certificates.Find(findType, findValue, validOnly: false);

            if (certificates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No certificate found matching {findType}='{findValue}' in {storeLocation}\\{storeName} store.");
            }

            if (certificates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple certificates ({certificates.Count}) found matching {findType}='{findValue}' in {storeLocation}\\{storeName} store. Please provide a more specific search criteria.");
            }

            // Return the first (and only) certificate
            return certificates[0];
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