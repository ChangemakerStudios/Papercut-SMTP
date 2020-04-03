// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Core.Infrastructure.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.Net;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;

    using Serilog;

    public static class NetworkHelper
    {
        const string NetworkAdapterQuery =
            "SELECT IPAddress from Win32_NetworkAdapterConfiguration WHERE IPEnabled=true";

        static readonly Regex _ipv4 = new Regex(
            @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$",
            RegexOptions.Compiled);

        public static string GetLocalDnsHostName()
        {
            string hostName = "localhost";
            try
            {
                hostName = Dns.GetHostName().ToLower();
            }
            catch (SocketException socketException)
            {
                Log.Logger.Warning(socketException, "Failure Getting the Local Hostname");
            }

            return hostName;
        }

        public static IEnumerable<string> GetIPAddresses()
        {
            try
            {
                var ips = new List<string>();

                using (var managementObjectSearcher = new ManagementObjectSearcher(NetworkAdapterQuery))
                using (var mgtObjects = managementObjectSearcher.Get())
                {
                    IEnumerable<PropertyData> addresses =
                        mgtObjects.OfType<ManagementObject>()
                            .Select(mo => mo.Properties["IPAddress"])
                            .Where(ip => ip.IsLocal)
                            .ToList();

                    foreach (PropertyData ipAddress in addresses)
                    {
                        if (ipAddress.IsArray) ips.AddRange((string[])ipAddress.Value);
                        else ips.Add(ipAddress.Value.ToString());
                    }
                }

                return ips.Where(address => address.IsValidIP()).ToList();
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex,
                    "Failure obtaining Local IP address(es). Most likely due to permissions. Run as elevated (Administrator) to access all local IP addresses.");
            }

            return new[] { "127.0.0.1" };
        }

        public static bool IsValidIP(this string ip)
        {
            if (ip == null) return false;
            return _ipv4.IsMatch(ip);
        }
    }
}