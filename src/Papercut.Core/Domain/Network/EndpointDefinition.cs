﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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

namespace Papercut.Core.Domain.Network
{
    public class EndpointDefinition
    {
        public EndpointDefinition(string address, int port)
        {
            this.Address = this.ParseIpAddress(address);
            this.Port = port;
        }

        public IPAddress Address { get; }

        public int Port { get; }

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

        public override string ToString()
        {
            return $"{this.Address}:{this.Port}";
        }
    }
}