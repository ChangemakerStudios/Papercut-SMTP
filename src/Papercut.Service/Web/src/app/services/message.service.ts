import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Message, MessageResponse, MessageDetail } from './message.repository';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private readonly apiUrl = '/api/messages';

  constructor(private http: HttpClient) {}

  // Fetch a paginated list of messages
  getMessages(limit = 10, start = 0): Observable<MessageResponse> {
    return this.http.get<MessageResponse>(this.apiUrl, {
      params: { limit, start }
    });
  }

  // Fetch a single message by ID
  getMessage(id: string): Observable<MessageDetail> {
    return this.http.get<MessageDetail>(`${this.apiUrl}/${id}`);
  }

  // Parse or sanitize HTML content (stub for now)
  parseHtml(html: string): string {
    // You can use a library like DOMPurify here if needed
    // return DOMPurify.sanitize(html);
    return html;
  }

  // Returns the HTML content for a message, handling plain text and HTML bodies
  getMessageContent(message: MessageDetail): string {
    if (!message.htmlBody && message.textBody) {
      return `
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            body { 
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
              line-height: 1.6; 
              color: #333; 
              margin: 16px; 
              background: white;
            }
            pre { white-space: pre-wrap; word-wrap: break-word; }
          </style>
        </head>
        <body>
          <pre>${this.escapeHtml(message.textBody)}</pre>
        </body>
        </html>
      `;
    }
    let content = message.htmlBody || '';
    if (content) {
      content = this.transformCidReferences(content, message.id);
      content = this.makeUrlsAbsolute(content);
    }
    if (content.includes('<html') || content.includes('<HTML')) {
      return content;
    } else {
      return `
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            body { 
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
              line-height: 1.6; 
              color: #333; 
              margin: 16px; 
              background: white;
            }
          </style>
        </head>
        <body>
          ${content}
        </body>
        </html>
      `;
    }
  }

  private escapeHtml(unsafe: string): string {
    return unsafe
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  private transformCidReferences(html: string, messageId: string): string {
    if (!html || !messageId) {
      return html;
    }
    return html.replace(/src=["']cid:([^"']+)["']/gi, (match, contentId) => {
      const encodedMessageId = encodeURIComponent(messageId);
      const encodedContentId = encodeURIComponent(contentId);
      return `src="/api/messages/${encodedMessageId}/contents/${encodedContentId}"`;
    });
  }

  private makeUrlsAbsolute(html: string): string {
    if (!html) return html;
    const baseUrl = window.location.origin;
    return html.replace(/src=["']\/api\/([^"']+)["']/gi, (match, apiPath) => {
      return `src="${baseUrl}/api/${apiPath}"`;
    });
  }
}

// Add interfaces for MessageResponse and MessageDetail as needed, or import them if already defined. 