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
    /// Common values: FindByThumbprint, FindBySubjectName, FindBySubjectDistinguishedName
    /// </summary>
    public string CertificateFindType { get; set; } = "FindByThumbprint";

    /// <summary>
    /// Certificate find value (e.g., thumbprint hash or subject name).
    /// Leave empty to disable TLS/STARTTLS.
    /// </summary>
    public string CertificateFindValue { get; set; } = string.Empty;

    /// <summary>
    /// Certificate store location (LocalMachine or CurrentUser).
    /// Default: LocalMachine
    /// </summary>
    public string CertificateStoreLocation { get; set; } = StoreLocation.LocalMachine.ToString();

    /// <summary>
    /// Certificate store name (My, Root, TrustedPeople, etc.).
    /// Default: My (Personal certificates)
    /// </summary>
    public string CertificateStoreName { get; set; } = StoreName.My.ToString();

    /// <summary>
    /// Base path where messages are written
    /// </summary>
    public string MessagePath { get; set; } = "%BaseDirectory%\\Incoming";

    /// <summary>
    /// Base path where logs are written.
    /// </summary>
    public string LoggingPath { get; set; } = @"%DataDirectory%\Logs;%BaseDirectory%\Logs";
}