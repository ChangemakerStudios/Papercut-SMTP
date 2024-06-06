﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


namespace Papercut.Service.Web.Models;

public class MimeMessageEntry : MessageEntry
{
    public MimeMessageEntry(MessageEntry entry, MimeMessage message)
        : base(entry.File)
    {
        this.MailMessage = message;
    }

    public string Subject => this.MailMessage?.Subject;

    public DateTime? Created => this._created;

    public string Id => this.Name;

    public MimeMessage MailMessage { get; }

    public class RefDto
    {
        public string Size { get; set; }

        public string Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Subject { get; set; }

        public static RefDto CreateFrom(MimeMessageEntry messageEntry)
        {
                return new RefDto
                {
                    Subject = messageEntry.Subject,
                    CreatedAt = messageEntry.Created?.ToUniversalTime(),
                    Id = messageEntry.Id,
                    Size = messageEntry.FileSize
                };
            }
    }

    public class DetailDto
    {
        public string Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Subject { get; set; }

        public List<EmailAddressDto> From { get; set; } = [];

        public List<EmailAddressDto> To { get; set; } = [];

        public List<EmailAddressDto> Cc { get; set; } = [];

        public List<EmailAddressDto> BCc { get; set; } = [];

        public string HtmlBody { get; set; }

        public string TextBody { get; set; }

        public List<HeaderDto> Headers { get; set; }

        public List<EmailAttachmentDto> Sections { get; set; }

        public static DetailDto CreateFrom(MimeMessageEntry messageEntry)
        {
                var mail = messageEntry.MailMessage;

                return new DetailDto
                {
                    Subject = messageEntry.Subject,
                    CreatedAt = messageEntry.Created?.ToUniversalTime(),
                    Id = messageEntry.Id,
                    From = ToAddressList(mail?.From),
                    To = ToAddressList(mail?.To),
                    Cc = ToAddressList(mail?.Cc),
                    BCc = ToAddressList(mail?.Bcc),
                    HtmlBody = mail?.HtmlBody,
                    TextBody = mail?.TextBody,
                    Headers = (mail?.Headers ?? []).Select(h => new HeaderDto { Name = h.Field, Value = h.Value}).ToList(),
                    Sections = ToSectionDtos(mail?.BodyParts)
                };
            }

        static List<EmailAttachmentDto> ToSectionDtos(IEnumerable<MimeEntity> bodyParts)
        {

                if (bodyParts == null) return [];

                return bodyParts
                    .OfType<MimePart>()
                    .Select(e => new EmailAttachmentDto
                    {
                        Id = e.ContentId,
                        MediaType = $"{e.ContentType.MediaType}/{e.ContentType.MediaSubtype}",
                        FileName = e.FileName
                    }).ToList();
            }

        static List<EmailAddressDto> ToAddressList(IList<InternetAddress> mailAddresses)
        {
                if (mailAddresses == null)
                {
                    return [];
                }

                return mailAddresses
                    .OfType<MailboxAddress>()
                    .Select(f => new EmailAddressDto {Address = f.Address, Name = f.Name})
                    .ToList();
            }
    }

    public class EmailAddressDto
    {
        public string Name { get; set; }

        public string Address { get; set; }
    }

    public class HeaderDto
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class EmailAttachmentDto
    {
        public string Id { get; set; }

        public string MediaType { get; set; }

        public string FileName { get; set; }
    }
}