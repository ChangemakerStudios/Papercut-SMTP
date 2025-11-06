using MailKit.Security;

namespace Papercut.Examples;

/// <summary>
/// Configuration options for SMTP sending in example applications
/// </summary>
public class SmtpSendOptions
{
    /// <summary>
    /// SMTP server hostname or IP address (default: 127.0.0.1)
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// SMTP server port (default: 25)
    /// Common ports: 25 (plain), 587 (STARTTLS), 465 (TLS)
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Security mode for SMTP connection
    /// </summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.None;

    /// <summary>
    /// Username for SMTP authentication (optional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for SMTP authentication (optional)
    /// </summary>
    public string? Password { get; set; }
}
