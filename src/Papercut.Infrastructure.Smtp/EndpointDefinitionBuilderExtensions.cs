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


using Papercut.Core.Domain.Network;

using SmtpServer;

namespace Papercut.Infrastructure.Smtp;

internal static class EndpointDefinitionBuilderExtensions
{
    /// <summary>
    /// Configures the SmtpServer endpoint builder with Papercut's endpoint definition,
    /// including optional TLS certificate support.
    /// </summary>
    /// <param name="builder">The SmtpServer endpoint builder</param>
    /// <param name="smtpEndpoint">Papercut endpoint definition with optional certificate</param>
    /// <returns>The configured builder for method chaining</returns>
    public static EndpointDefinitionBuilder WithEndpoint(
        this EndpointDefinitionBuilder builder,
        EndpointDefinition smtpEndpoint)
    {
        builder = builder.Endpoint(smtpEndpoint.ToIPEndPoint());

        // Add certificate if configured for TLS/STARTTLS support
        if (smtpEndpoint.Certificate != null)
        {
            builder = builder.Certificate(smtpEndpoint.Certificate);
        }

        return builder;
    }
}
