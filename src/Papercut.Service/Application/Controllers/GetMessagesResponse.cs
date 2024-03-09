namespace Papercut.Service.Application.Controllers;

using System.Collections.Generic;

using Papercut.Service.Web.Models;

public class GetMessagesResponse
{
    public int TotalMessageCount { get; }

    public List<MimeMessageEntry.RefDto> Messages { get; }

    public GetMessagesResponse(int totalMessageCount, List<MimeMessageEntry.RefDto> messages)
    {
        TotalMessageCount = totalMessageCount;
        Messages = messages;
    }
}