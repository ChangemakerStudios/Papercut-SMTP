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

using Infrastructure.EmailAddresses;

[PublicAPI]
public class DetailDto : RefDto
{
    public List<EmailAddressDto> To { get; set; } = [];

    public List<EmailAddressDto> Cc { get; set; } = [];

    public List<EmailAddressDto> BCc { get; set; } = [];

    public string? HtmlBody { get; set; }

    public string? TextBody { get; set; }

    public List<HeaderDto> Headers { get; set; } = [];

    public List<EmailSectionDto> Sections { get; set; } = [];

    public List<EmailSectionDto> Attachments { get; set; } = [];

    public new static DetailDto CreateFrom(MimeMessageEntry messageEntry)
    {
        var mail = messageEntry.MailMessage;

        var sections = ToSectionDtos(mail?.BodyParts);

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
            Sections = sections,
            Attachments = sections.Where(s => s.IsAttachment).ToList()
        };
    }

    // Sections are indexed against the full BodyParts list so Index lines up with
    // the "api/messages/{id}/sections/{index}" endpoint; Attachments is the subset.
    private static List<EmailSectionDto> ToSectionDtos(IEnumerable<MimeEntity>? bodyParts)
    {
        if (bodyParts == null) return [];

        return bodyParts
            .OfType<MimePart>()
            .Select((e, i) => new EmailSectionDto
            {
                Index = i,
                Id = e.ContentId,
                MediaType = $"{e.ContentType.MediaType}/{e.ContentType.MediaSubtype}",
                FileName = e.FileName,
                IsAttachment = e.IsAttachment,
                Size = GetContentSize(e)
            }).ToList();
    }

    private static long? GetContentSize(MimePart part)
    {
        try
        {
            var stream = part.Content?.Stream;
            return stream?.CanSeek == true ? stream.Length : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
