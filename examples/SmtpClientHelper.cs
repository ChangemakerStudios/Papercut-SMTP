namespace Papercut.Examples;

using MailKit.Net.Smtp;
using MailKit.Security;

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

        try
        {
            var secureSocketOptions = MapSecurityMode(options.Security);
            await client.ConnectAsync(options.Host, options.Port, secureSocketOptions, cancellationToken);

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

    /// <summary>
    /// Maps SmtpSecurityMode to MailKit's SecureSocketOptions
    /// </summary>
    /// <param name="securityMode">Security mode from configuration</param>
    /// <returns>Corresponding SecureSocketOptions</returns>
    public static SecureSocketOptions MapSecurityMode(SmtpSecurityMode securityMode)
    {
        return securityMode switch
        {
            SmtpSecurityMode.None => SecureSocketOptions.None,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };
    }
}
