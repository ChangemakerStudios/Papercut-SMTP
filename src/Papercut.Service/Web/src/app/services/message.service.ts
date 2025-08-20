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
import { MessageRepository } from './message.repository';

/**
 * Service for managing email messages.
 * Provides methods to interact with the Papercut message API.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private readonly apiUrl = '/api/messages';

  constructor(
    private http: HttpClient,
    private messageRepository: MessageRepository
  ) {}

  /**
   * Gets a paginated list of messages.
   * @param limit The maximum number of messages to return (default: 10)
   * @param start The starting index for pagination (default: 0)
   * @returns Observable of GetMessagesResponse
   */
  getMessages(limit: number = 10, start: number = 0): Observable<GetMessagesResponse> {
    return this.messageRepository.getMessages({ limit, start })
      .pipe(
        map(response => ({
          ...response,
          messages: (response.messages || []).map(msg => ({
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
    return this.messageRepository.getMessage(messageId)
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
    this.messageRepository.downloadRawMessage(messageId);
  }

  /**
   * Downloads a specific message section by index.
   * @param messageId The unique message ID
   * @param sectionIndex The zero-based section index
   */
  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    this.messageRepository.downloadSectionByIndex(messageId, sectionIndex);
  }

  /**
   * Downloads a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   */
  downloadSectionByContentId(messageId: string, contentId: string): void {
    this.messageRepository.downloadSectionByContentId(messageId, contentId);
  }

  /**
   * Downloads raw message file with progress tracking.
   * @param messageId The unique message ID
   */
  downloadRawMessageWithProgress(messageId: string): void {
    // This will be handled by FileDownloaderService
    // For now, use the repository method as fallback
    this.messageRepository.downloadRawMessage(messageId);
  }

  /**
   * Gets the content of a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   * @returns Observable of the section content as text
   */
  getSectionContent(messageId: string, contentId: string): Observable<string> {
    return this.messageRepository.getSectionContent(messageId, contentId);
  }

  /**
   * Gets the content of a specific message section by index.
   * @param messageId The unique message ID
   * @param index The index of the section in the sections array
   * @returns Observable of the section content as text
   */
  getSectionByIndex(messageId: string, index: number): Observable<string> {
    return this.messageRepository.getSectionByIndex(messageId, index);
  }

  /**
   * Gets the raw message content.
   * @param messageId The unique message ID
   * @returns Observable of the raw message content as text
   */
  getRawContent(messageId: string): Observable<string> {
    return this.messageRepository.getRawContent(messageId);
  }

  /**
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.messageRepository.deleteAllMessages();
  }



  // Parse or sanitize HTML content (stub for now)
  parseHtml(html: string): string {
    // You can use a library like DOMPurify here if needed
    // return DOMPurify.sanitize(html);
    return html;
  }

    // Returns the HTML content for a message, handling plain text and HTML bodies
  getMessageContent(message: DetailDto): string {
    const content = message.htmlBody || message.textBody || '';
    const mediaType = message.htmlBody ? 'text/html' : 'text/plain';
    return this.formatMessageContent(content, mediaType, message.id ?? '');
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

  /**
   * Formats content for display in iframe with proper styling and theme support.
   * Used by both message content and section content display.
   * @param content The raw content
   * @param mediaType The media type of the content
   * @param messageId The message ID for CID reference transformation
   * @returns Formatted HTML content ready for iframe display
   */
  formatMessageContent(content: string, mediaType: string, messageId: string = ''): string {
    if (!content) {
      return this.createStyledDocument('Loading...', true);
    }

    const lowerMediaType = (mediaType || '').toLowerCase();
    
    if (lowerMediaType === 'text/html') {
      // Process HTML content with CID and URL transformations
      let processedContent = content;
      if (messageId) {
        processedContent = this.transformCidReferences(processedContent, messageId);
        processedContent = this.makeUrlsAbsolute(processedContent);
      }
      
      // If it's a complete HTML document, inject theme styles
      if (processedContent.includes('<html') || processedContent.includes('<HTML')) {
        return this.injectThemeStyles(processedContent);
      } else {
        // Wrap partial HTML in complete document
        return this.createStyledDocument(processedContent, false);
      }
    } else {
      // For text/plain and other text types
      const escapedContent = this.escapeHtml(content);
      return this.createStyledDocument(`<pre>${escapedContent}</pre>`, true);
    }
  }

  /**
   * @deprecated Use formatMessageContent instead
   * Formats section content for display in iframe with proper styling and theme support.
   */
  formatSectionContent(content: string, mediaType: string, messageId: string): string {
    return this.formatMessageContent(content, mediaType, messageId);
  }

  private createStyledDocument(content: string, isPreformatted: boolean): string {
    const themeStyles = this.getThemeAwareStyles();
    const bodyContent = isPreformatted ? content : content;
    
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        ${themeStyles}
      </head>
      <body>
        ${bodyContent}
      </body>
      </html>
    `;
  }

  private injectThemeStyles(html: string): string {
    const themeStyles = this.getThemeAwareStyles();
    
    if (html.includes('<head>')) {
      return html.replace('<head>', `<head>${themeStyles}`);
    } else {
      // If no head tag, wrap the content
      return this.createStyledDocument(html, false);
    }
  }

  private getThemeAwareStyles(): string {
    // Detect theme by checking the 'dark' class on documentElement (set by ThemeService)
    // Also check data-theme attribute on body as fallback
    const isDarkMode = document.documentElement.classList.contains('dark') || 
                      document.body.getAttribute('data-theme') === 'dark';
    
    const textColor = isDarkMode ? '#ffffff' : '#333333';
    const bgColor = isDarkMode ? '#1f2937' : '#ffffff';
    const linkColor = isDarkMode ? '#60a5fa' : '#0066cc';
    
    // Theme detection completed for email rendering
    
    return `<style>
      html, body { 
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
        line-height: 1.6 !important;
        background: ${bgColor} !important; 
        color: ${textColor} !important; 
        fill: ${textColor} !important;
        margin: 0 !important;
        padding: 4px !important;
        min-height: 100vh !important;
      }
      * { 
        color: ${textColor} !important; 
        fill: ${textColor} !important;
        background-color: transparent !important;
      }
      p, div, span, h1, h2, h3, h4, h5, h6, td, th, li {
        color: ${textColor} !important;
        fill: ${textColor} !important;
        background-color: transparent !important;
      }
      a { 
        color: ${linkColor} !important; 
        fill: ${linkColor} !important;
      }
      pre {
        font-family: 'Courier New', monospace !important;
        white-space: pre-wrap !important;
        word-wrap: break-word !important;
        color: ${textColor} !important;
        fill: ${textColor} !important;
        background-color: transparent !important;
        margin: 0 !important;
        padding: 0 !important;
      }
      img {
        max-width: 100% !important;
        height: auto !important;
      }
      table {
        border-collapse: collapse !important;
        color: ${textColor} !important;
        fill: ${textColor} !important;
      }
      table td, table th {
        border: 1px solid ${isDarkMode ? '#4b5563' : '#d1d5db'} !important;
        padding: 8px !important;
        color: ${textColor} !important;
        fill: ${textColor} !important;
      }
    </style>`;
  }
} 