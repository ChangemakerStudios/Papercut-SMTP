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


namespace Papercut.Service.Domain.Messages;

/// <summary>
///     Parameters for forwarding a captured message to a real SMTP server,
///     mirroring the desktop app's Forward Message dialog.
/// </summary>
[PublicAPI]
public class ForwardMessageRequest
{
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 25;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string ToEmail { get; set; } = string.Empty;
}
