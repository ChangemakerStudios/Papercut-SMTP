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

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { 
  GetMessagesResponse, 
  DetailDto, 
  RefDto 
} from '../models';

/**
 * Service for managing email messages.
 * Provides methods to interact with the Papercut message API.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private readonly apiUrl = '/api/messages';

  constructor(private http: HttpClient) {}

  /**
   * Gets a paginated list of messages.
   * @param limit The maximum number of messages to return (default: 10)
   * @param start The starting index for pagination (default: 0)
   * @returns Observable of GetMessagesResponse
   */
  getMessages(limit: number = 10, start: number = 0): Observable<GetMessagesResponse> {
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('start', start.toString());

    return this.http.get<GetMessagesResponse>(this.apiUrl, { params })
      .pipe(
        map(response => ({
          ...response,
          messages: response.messages.map(msg => ({
            ...msg,
            createdAt: msg.createdAt ? new Date(msg.createdAt) : null
          }))
        }))
      );
  }

  /**
   * Gets the basic RefDto information for a specific message.
   * This is faster than getMessage() as it queries the list endpoint.
   * @param messageId The unique message ID
   * @returns Observable of RefDto or null if not found
   */
  getMessageRef(messageId: string): Observable<RefDto | null> {
    // Get recent messages and find the one with matching ID
    return this.getMessages(50, 0).pipe(
      map(response => {
        const found = response.messages.find(msg => msg.id === messageId);
        return found || null;
      })
    );
  }

  /**
   * Gets the detailed information for a specific message.
   * @param messageId The unique message ID
   * @returns Observable of DetailDto
   */
  getMessage(messageId: string): Observable<DetailDto> {
    const encodedId = encodeURIComponent(messageId);
    return this.http.get<DetailDto>(`${this.apiUrl}/${encodedId}`)
      .pipe(
        map(detail => ({
          ...detail,
          createdAt: detail.createdAt ? new Date(detail.createdAt) : null
        }))
      );
  }

  /**
   * Downloads the raw message file.
   * @param messageId The unique message ID
   */
  downloadRawMessage(messageId: string): void {
    const encodedId = encodeURIComponent(messageId);
    const url = `${this.apiUrl}/${encodedId}/raw`;
    window.open(url, '_blank');
  }

  /**
   * Downloads a specific message section by index.
   * @param messageId The unique message ID
   * @param sectionIndex The zero-based section index
   */
  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    const encodedId = encodeURIComponent(messageId);
    const url = `${this.apiUrl}/${encodedId}/sections/${sectionIndex}`;
    window.open(url, '_blank');
  }

  /**
   * Downloads a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   */
  downloadSectionByContentId(messageId: string, contentId: string): void {
    const encodedId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    const url = `${this.apiUrl}/${encodedId}/contents/${encodedContentId}`;
    window.open(url, '_blank');
  }

  /**
   * Downloads raw message file with progress tracking.
   * @param messageId The unique message ID
   */
  downloadRawMessageWithProgress(messageId: string): void {
    // This will be handled by FileDownloaderService
    const encodedId = encodeURIComponent(messageId);
    const url = `${this.apiUrl}/${encodedId}/raw`;
    window.open(url, '_blank'); // Fallback for now
  }

  /**
   * Gets the content of a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   * @returns Observable of the section content as text
   */
  getSectionContent(messageId: string, contentId: string): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    const url = `${this.apiUrl}/${encodedId}/contents/${encodedContentId}`;
    return this.http.get(url, { responseType: 'text' });
  }

  /**
   * Gets the content of a specific message section by index.
   * @param messageId The unique message ID
   * @param index The index of the section in the sections array
   * @returns Observable of the section content as text
   */
  getSectionByIndex(messageId: string, index: number): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    const url = `${this.apiUrl}/${encodedId}/sections/${index}`;
    return this.http.get(url, { responseType: 'text' });
  }

  /**
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }



  // Parse or sanitize HTML content (stub for now)
  parseHtml(html: string): string {
    // You can use a library like DOMPurify here if needed
    // return DOMPurify.sanitize(html);
    return html;
  }

  // Returns the HTML content for a message, handling plain text and HTML bodies
  getMessageContent(message: DetailDto): string {
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
      content = this.transformCidReferences(content, message.id ?? '');
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