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


using Papercut.Service.Domain.Models;
using Papercut.Service.Infrastructure.EmailAddresses;

[PublicAPI]
public class DetailDto
{
    public string? Id { get; set; }
    
    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Subject { get; set; }

    public List<EmailAddressDto> From { get; set; } = [];

    public List<EmailAddressDto> To { get; set; } = [];

    public List<EmailAddressDto> Cc { get; set; } = [];

    public List<EmailAddressDto> BCc { get; set; } = [];

    public string? HtmlBody { get; set; }

    public string? TextBody { get; set; }

    public List<HeaderDto> Headers { get; set; } = [];

    public List<EmailSectionDto> Sections { get; set; } = [];
    
    public List<EmailSectionDto> Attachments { get; set; } = [];

    public static DetailDto CreateFrom(MimeMessageEntry messageEntry)
    {
        var mail = messageEntry.MailMessage;

        return new DetailDto
        {
            Id = messageEntry.Id,
            Name = messageEntry.Name,
            Subject = messageEntry.Subject,
            CreatedAt = messageEntry.Created?.ToUniversalTime(),
            From = (mail?.From).ToAddressList(),
            To = (mail?.To).ToAddressList(),
            Cc = (mail?.Cc).ToAddressList(),
            BCc = (mail?.Bcc).ToAddressList(),
            HtmlBody = mail?.HtmlBody,
            TextBody = mail?.TextBody,
            Headers = (mail?.Headers ?? [])
                .Select(h => new HeaderDto { Name = h.Field, Value = h.Value }).ToList(),
            Sections = ToSectionDtos(mail?.BodyParts),
            Attachments = ToSectionDtos(mail?.Attachments)
        };
    }

    private static List<EmailSectionDto> ToSectionDtos(IEnumerable<MimeEntity>? bodyParts)
    {
        if (bodyParts == null) return [];

        return bodyParts
            .OfType<MimePart>()
            .Select(e => new EmailSectionDto
            {
                Id = e.ContentId,
                MediaType = $"{e.ContentType.MediaType}/{e.ContentType.MediaSubtype}",
                FileName = e.FileName
            }).ToList();
    }
}