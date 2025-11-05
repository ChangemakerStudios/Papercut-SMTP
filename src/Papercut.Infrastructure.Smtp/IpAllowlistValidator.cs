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
using System.Net.Sockets;

namespace Papercut.Infrastructure.Smtp;

public class IpAllowlistValidator
{
    private readonly List<IpRange> _allowedRanges = new();
    private readonly bool _allowAll;

    public IpAllowlistValidator(string allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            _allowAll = true;
            return;
        }

        // Parse comma-separated list of IP addresses and CIDR ranges
        var entries = allowedHosts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var entry in entries)
        {
            if (entry == "*")
            {
                _allowAll = true;
                _allowedRanges.Clear();
                return;
            }

            if (entry.Contains('/'))
            {
                // CIDR notation
                _allowedRanges.Add(ParseCidr(entry));
            }
            else
            {
                // Single IP address
                if (IPAddress.TryParse(entry, out var ip))
                {
                    _allowedRanges.Add(new IpRange(ip, ip));
                }
            }
        }
    }

    public bool IsAllowed(IPAddress ipAddress)
    {
        if (_allowAll)
        {
            return true;
        }

        // Allow IPv6 loopback and IPv4 loopback
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        // Convert IPv4-mapped IPv6 addresses to IPv4
        if (ipAddress.IsIPv4MappedToIPv6)
        {
            ipAddress = ipAddress.MapToIPv4();
        }

        return _allowedRanges.Any(range => range.Contains(ipAddress));
    }

    private static IpRange ParseCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid CIDR notation: {cidr}");
        }

        if (!IPAddress.TryParse(parts[0], out var baseAddress))
        {
            throw new ArgumentException($"Invalid IP address in CIDR: {parts[0]}");
        }

        if (!int.TryParse(parts[1], out var prefixLength))
        {
            throw new ArgumentException($"Invalid prefix length in CIDR: {parts[1]}");
        }

        var addressBytes = baseAddress.GetAddressBytes();
        var isIpv4 = baseAddress.AddressFamily == AddressFamily.InterNetwork;
        var maxPrefixLength = isIpv4 ? 32 : 128;

        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            throw new ArgumentException($"Prefix length must be between 0 and {maxPrefixLength} for {(isIpv4 ? "IPv4" : "IPv6")}");
        }

        // Calculate network address (base) and broadcast address (end)
        var maskBytes = CreateMaskBytes(addressBytes.Length, prefixLength);
        var networkBytes = new byte[addressBytes.Length];
        var broadcastBytes = new byte[addressBytes.Length];

        for (int i = 0; i < addressBytes.Length; i++)
        {
            networkBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
            broadcastBytes[i] = (byte)(addressBytes[i] | ~maskBytes[i]);
        }

        var networkAddress = new IPAddress(networkBytes);
        var broadcastAddress = new IPAddress(broadcastBytes);

        return new IpRange(networkAddress, broadcastAddress);
    }

    private static byte[] CreateMaskBytes(int length, int prefixLength)
    {
        var maskBytes = new byte[length];
        var remainingBits = prefixLength;

        for (int i = 0; i < length; i++)
        {
            if (remainingBits >= 8)
            {
                maskBytes[i] = 0xFF;
                remainingBits -= 8;
            }
            else if (remainingBits > 0)
            {
                maskBytes[i] = (byte)(0xFF << (8 - remainingBits));
                remainingBits = 0;
            }
            else
            {
                maskBytes[i] = 0x00;
            }
        }

        return maskBytes;
    }

    private readonly record struct IpRange(IPAddress Start, IPAddress End)
    {
        public bool Contains(IPAddress address)
        {
            // Ensure same address family
            if (address.AddressFamily != Start.AddressFamily)
            {
                return false;
            }

            var addressBytes = address.GetAddressBytes();
            var startBytes = Start.GetAddressBytes();
            var endBytes = End.GetAddressBytes();

            // Check if address is within range
            for (int i = 0; i < addressBytes.Length; i++)
            {
                if (addressBytes[i] < startBytes[i])
                {
                    return false;
                }

                if (addressBytes[i] > endBytes[i])
                {
                    return false;
                }

                // If this byte is strictly between start and end, we're definitely in range
                if (addressBytes[i] > startBytes[i] && addressBytes[i] < endBytes[i])
                {
                    return true;
                }
            }

            return true;
        }
    }
}
