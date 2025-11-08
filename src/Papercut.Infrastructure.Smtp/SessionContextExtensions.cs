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

using SmtpServer;

namespace Papercut.Infrastructure.Smtp;

/// <summary>
/// Extension methods for ISessionContext to extract connection information.
/// </summary>
public static class SessionContextExtensions
{
    /// <summary>
    /// Gets the remote IP address from the session context.
    /// </summary>
    /// <param name="context">The SMTP session context</param>
    /// <returns>The remote IP address, or IPAddress.None if it cannot be determined</returns>
    public static IPAddress GetRemoteIpAddress(this ISessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        // RemoteEndPoint is stored in the Properties dictionary by SmtpServer
        return context.Properties.TryGetValue("EndpointListener:RemoteEndPoint", out var endpoint)
            && endpoint is IPEndPoint remoteEndPoint
                ? remoteEndPoint.Address
                : IPAddress.None;
    }
}
