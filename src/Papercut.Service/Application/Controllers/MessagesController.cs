// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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


using Papercut.Service.Web;

namespace Papercut.Service.Application.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using MimeKit;

using Papercut.Common.Extensions;
using Papercut.Message;
using Papercut.Message.Helpers;
using Papercut.Service.Web.Models;
using Serilog;

[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    readonly MimeMessageLoader _messageLoader;

    private readonly ILogger _logger;

    readonly MessageRepository _messageRepository;

    public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader, ILogger logger)
    {
        _messageRepository = messageRepository;
        _messageLoader = messageLoader;
        _logger = logger;
    }

    [HttpGet]
    public GetMessagesResponse GetAll(int limit = 10, int start = 0)
    {
        var messageEntries = _messageRepository.LoadMessages();

        var messages = messageEntries
            .OrderByDescending(msg => msg.ModifiedDate)
            .Skip(start)
            .Take(limit)
            .Select(e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, _messageLoader.LoadMailMessage(e))))
            .ToList();

        return new GetMessagesResponse(messageEntries.Count, messages);
    }

    [HttpDelete]
    public void DeleteAll()
    {
        _messageRepository.LoadMessages()
            .ForEach(
                msg =>
                {
                    try
                    {
                        _messageRepository.DeleteMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failure Deleting Message File {MessageFile}", msg.File);
                    }
                });
    }

    [HttpGet("{id}")]
    public object Get(string id)
    {
        var messageEntry = _messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
        if (messageEntry == null)
        {
            return NotFound();
        }

        var dto = MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, _messageLoader.LoadMailMessage(messageEntry)));
        return dto;
    }

    [HttpGet("{messageId}/raw")]
    public IActionResult DownloadRaw(string messageId)
    {
        var messageEntry = _messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
        if (messageEntry == null)
        {
            return NotFound();
        }

        var response = new FileStreamResult(System.IO.File.OpenRead(messageEntry.File), "message/rfc822");
        response.FileDownloadName = Uri.EscapeDataString(messageId);
        return response;
    }

    [HttpGet("{messageId}/sections/{index}")]
    public IActionResult DownloadSection(string messageId, int index)
    {
        return DownloadSection(messageId, sections => index >= 0 && index < sections.Count ? sections[index] : null);
    }

    [HttpGet("{messageId}/contents/{contentId}")]
    public IActionResult DownloadSectionContent(string messageId, string contentId)
    {
        return DownloadSection(messageId, sections => sections.FirstOrDefault(s => s.ContentId == contentId));
    }

    IActionResult DownloadSection(string messageId, Func<List<MimePart>, MimePart> findSection)
    {
        var messageEntry = _messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
        if (messageEntry == null)
        {
            return NotFound();
        }

        var mimeMessage = new MimeMessageEntry(messageEntry, _messageLoader.LoadMailMessage(messageEntry));
        var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

        var mimePart = findSection(sections);
        if (mimePart == null)
        {
            return NotFound();
        }

        var response = new MimePartFileStreamResult(
            mimePart.Content,
            $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
        var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
        response.FileDownloadName = Uri.EscapeDataString(FileHelper.NormalizeFilename(filename));
        return response;
    }
}