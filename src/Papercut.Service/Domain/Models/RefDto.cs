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


using Papercut.Service.Infrastructure.EmailAddresses;

namespace Papercut.Service.Domain.Models;

[PublicAPI]
public class RefDto
{
    public string? Id { get; set; }
    
    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Subject { get; set; }
    
    public long Size { get; set; }

    public List<EmailAddressDto> From { get; set; } = [];

    public static RefDto CreateFrom(MimeMessageEntry messageEntry)
    {
        return new RefDto
        {
            Id = messageEntry.Id,
            Name = messageEntry.Name,
            Subject = messageEntry.Subject,
            CreatedAt = messageEntry.Created?.ToUniversalTime(),
            Size = messageEntry.FileSize,
            From = (messageEntry.MailMessage?.From).ToAddressList()
        };
    }
}