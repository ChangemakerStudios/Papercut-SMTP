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


namespace Papercut.Infrastructure.IPComm
{
    using System;

    using Network;

    using Serilog;

    public enum PapercutIPCommClientConnectTo
    {
        UI,
        Service
    }

    public class PapercutIPCommClientFactory
    {
        private readonly PapercutIPCommEndpoints _endpoints;
        private readonly ILogger _logger;

        public PapercutIPCommClientFactory(PapercutIPCommEndpoints endpoints, ILogger logger)
        {
            _endpoints = endpoints;
            _logger = logger;
        }

        public PapercutIPCommClient GetClient(PapercutIPCommClientConnectTo connectTo)
        {
            switch (connectTo)
            {
                case PapercutIPCommClientConnectTo.Service:
                    return new PapercutIPCommClient(_endpoints.Service, _logger.ForContext<PapercutIPCommClient>());
                case PapercutIPCommClientConnectTo.UI:
                    return new PapercutIPCommClient(_endpoints.UI, _logger.ForContext<PapercutIPCommClient>());
            }

            throw new ArgumentOutOfRangeException($"Connect to is unknown: {connectTo}");
        }
    }
}