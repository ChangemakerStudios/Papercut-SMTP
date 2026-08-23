// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


namespace Papercut.Service.TrayNotification.Infrastructure;

/// <summary>
/// The subset of the service's RefDto the tray needs off the "NewMessageReceived"
/// hub message. Deliberately a local shape rather than a reference to
/// Papercut.Service -- the tray is a client of the HTTP surface, not of the
/// service assembly.
/// </summary>
[PublicAPI]
public class NewMessageDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Subject { get; set; }

    public List<EmailAddressDto> From { get; set; } = [];

    /// <summary>
    /// Display name for the sender, preferring the friendly name over the address.
    /// </summary>
    public string? FromDisplay
    {
        get
        {
            var first = From.FirstOrDefault();

            if (first == null) return null;

            return !string.IsNullOrWhiteSpace(first.Name) ? first.Name : first.Address;
        }
    }
}

[PublicAPI]
public class EmailAddressDto
{
    public string? Name { get; set; }

    public string? Address { get; set; }
}
