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


namespace Papercut.Examples;

using MailKit.Net.Smtp;

/// <summary>
/// Helper class for creating and configuring MailKit SmtpClient instances
/// </summary>
public static class SmtpClientHelper
{
    /// <summary>
    /// Creates and connects a MailKit SmtpClient with the specified options
    /// </summary>
    /// <param name="options">SMTP configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected and authenticated SmtpClient</returns>
    public static async Task<SmtpClient> CreateAndConnectAsync(
        SmtpSendOptions options,
        CancellationToken cancellationToken = default)
    {
        var client = new SmtpClient();

        return await client.CreateAndConnectAsync(options, cancellationToken);
    }

    /// <summary>
    /// Creates and connects a MailKit SmtpClient with the specified options
    /// </summary>
    /// <param name="client"></param>
    /// <param name="options">SMTP configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected and authenticated SmtpClient</returns>
    public static async Task<SmtpClient> CreateAndConnectAsync(this SmtpClient client,
        SmtpSendOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.ConnectAsync(options.Host, options.Port, options.Security, cancellationToken);

            if (!string.IsNullOrEmpty(options.Username))
            {
                await client.AuthenticateAsync(options.Username, options.Password ?? string.Empty, cancellationToken);
            }

            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}
