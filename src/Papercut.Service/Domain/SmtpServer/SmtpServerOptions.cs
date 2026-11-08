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


using System.Security.Cryptography.X509Certificates;

namespace Papercut.Service.Domain.SmtpServer;

public class SmtpServerOptions
{
    public string IP { get; set; } = "Any";

    public int Port { get; set; } = 25;

    /// <summary>
    /// Certificate find type for TLS/STARTTLS support.
    /// Recommended: FindBySubjectName (easiest - use certificate CN like "localhost")
    /// Alternative: FindByThumbprint (more specific but harder to configure)
    /// </summary>
    public string CertificateFindType { get; set; } = "FindBySubjectName";

    /// <summary>
    /// Certificate find value (e.g., "localhost" for subject name, or thumbprint hash).
    /// Leave empty to disable TLS/STARTTLS.
    /// Example: "localhost" when using FindBySubjectName
    /// </summary>
    public string CertificateFindValue { get; set; } = string.Empty;

    /// <summary>
    /// Certificate store location (LocalMachine or CurrentUser).
    /// Default: LocalMachine
    /// </summary>
    public string CertificateStoreLocation { get; set; } = nameof(StoreLocation.LocalMachine);

    /// <summary>
    /// Certificate store name (My, Root, TrustedPeople, etc.).
    /// Default: My (Personal certificates)
    /// </summary>
    public string CertificateStoreName { get; set; } = nameof(StoreName.My);

    /// <summary>
    /// Base path where messages are written
    /// </summary>
    public string MessagePath { get; set; } = "%BaseDirectory%\\Incoming";

    /// <summary>
    /// Base path where logs are written.
    /// </summary>
    public string LoggingPath { get; set; } = @"%DataDirectory%\Logs;%BaseDirectory%\Logs";

    /// <summary>
    /// Comma-separated list of allowed client IP addresses or CIDR ranges for SMTP connections.
    /// Use "*" to allow all hosts (default).
    /// Examples: "192.168.1.0/24,10.0.0.0/8" or "127.0.0.1,192.168.1.100"
    /// Environment variable: SmtpServer__AllowedIps
    /// </summary>
    public string AllowedIps { get; set; } = "*";

    /// <summary>
    /// Limits how many messages the SMTP server will accept per window, so applications
    /// can be tested against a mail server that is throttling them.
    /// Format is "&lt;count&gt;/&lt;window&gt;", where window is a number followed by
    /// s (seconds), m (minutes), or h (hours).
    /// Use "*" for no limit (default).
    /// Examples: "500/1h", "5/10m", "100/30s"
    /// Environment variable: SmtpServer__RateLimit
    /// </summary>
    public string RateLimit { get; set; } = "*";

    /// <summary>
    /// SMTP reply code returned once <see cref="RateLimit" /> is reached.
    /// 4xx codes are transient (the client should retry later), 5xx are permanent.
    /// Common choices: 421, 451 (default), 452, 550.
    /// Environment variable: SmtpServer__RateLimitReplyCode
    /// </summary>
    public int RateLimitReplyCode { get; set; } = 451;

    public SmtpServerSettings GetSettings(string ipOverride, int portOverride) => new(ipOverride,
        portOverride,
        CertificateFindType,
        CertificateFindValue,
        CertificateStoreLocation,
        CertificateStoreName,
        MessagePath,
        LoggingPath,
        AllowedIps,
        RateLimit,
        RateLimitReplyCode);
}
