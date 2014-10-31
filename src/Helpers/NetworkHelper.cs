// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;

    public static class NetworkHelper
    {
        const string NetworkAdapterQuery =
            "SELECT IPAddress from Win32_NetworkAdapterConfiguration WHERE IPEnabled=true";

        static readonly Regex _ipv4 = new Regex(
            @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$",
            RegexOptions.Compiled);

        public static IEnumerable<string> GetIPAddresses()
        {
            var ips = new List<string>();

            using (var managementObjectSearcher = new ManagementObjectSearcher(NetworkAdapterQuery))
            using (var mgtObjects = managementObjectSearcher.Get())
            {
                IEnumerable<PropertyData> addresses =
                    mgtObjects.OfType<ManagementObject>()
                        .Select(mo => mo.Properties["IPAddress"])
                        .Where(ip => ip.IsLocal);

                foreach (PropertyData ipAddress in addresses)
                {
                    if (ipAddress.IsArray) ips.AddRange((string[])ipAddress.Value);
                    else ips.Add(ipAddress.Value.ToString());
                }
            }

            return ips.Where(address => _ipv4.IsMatch(address)).ToList();
        }

        public static bool IsValidIP(string ip)
        {
            if (ip == null) return false;
            return _ipv4.IsMatch(ip);
        }
    }
}