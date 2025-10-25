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
    public SmtpSecurityMode Security { get; set; } = SmtpSecurityMode.None;

    /// <summary>
    /// Username for SMTP authentication (optional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for SMTP authentication (optional)
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// SMTP security modes
/// </summary>
public enum SmtpSecurityMode
{
    /// <summary>
    /// No encryption (plain SMTP on port 25)
    /// </summary>
    None,

    /// <summary>
    /// STARTTLS - Start with plain connection, upgrade to TLS (port 587)
    /// </summary>
    StartTls,

    /// <summary>
    /// Immediate TLS/SSL connection (port 465)
    /// </summary>
    SslOnConnect
}
