// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { Injectable } from '@angular/core';
import { ThemeService } from './theme.service';
import { ContentTransformationService } from './content-transformation.service';

/**
 * Service for formatting email content for display.
 * Handles HTML content formatting, theme-aware styling, and document creation.
 * This service focuses solely on content formatting operations.
 */
@Injectable({
  providedIn: 'root'
})
export class ContentFormattingService {

  constructor(
    private themeService: ThemeService,
    private contentTransformationService: ContentTransformationService
  ) {}

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
      // If it's a complete HTML document, inject theme styles
      if (content.includes('<html') || content.includes('<HTML')) {
        return this.injectThemeStyles(content);
      } else {
        // Wrap partial HTML in complete document
        return this.createStyledDocument(content, false);
      }
    } else {
      // For text/plain and other text types
      const escapedContent = this.escapeHtml(content);
      return this.createStyledDocument(`<pre>${escapedContent}</pre>`, true);
    }
  }

  /**
   * Formats section content for display in iframe with proper styling and theme support.
   * @param content The raw content
   * @param mediaType The media type of the content
   * @param messageId The message ID for CID reference transformation
   * @returns Formatted HTML content ready for iframe display
   */
  formatSectionContent(content: string, mediaType: string, messageId: string): string {
    return this.formatMessageContent(content, mediaType, messageId);
  }

  /**
   * Returns the HTML content for a message, handling plain text and HTML bodies.
   * @param htmlBody The HTML body content
   * @param textBody The text body content
   * @param messageId The message ID for formatting
   * @returns Formatted HTML content
   */
  getMessageContent(htmlBody: string | null, textBody: string | null, messageId: string): string {
    const content = htmlBody || textBody || '';
    const mediaType = htmlBody ? 'text/html' : 'text/plain';
    
    // Apply content transformations first if we have HTML content and a message ID
    let processedContent = content;
    if (messageId && htmlBody && mediaType === 'text/html') {
      processedContent = this.contentTransformationService.transformContent(content, messageId);
    }
    
    return this.formatMessageContent(processedContent, mediaType, messageId);
  }

  /**
   * Parse or sanitize HTML content.
   * @param html The HTML content to parse
   * @returns Parsed HTML content
   */
  parseHtml(html: string): string {
    // You can use a library like DOMPurify here if needed
    // return DOMPurify.sanitize(html);
    return html;
  }

  /**
   * Creates a complete HTML document with theme-aware styling.
   * @param content The content to wrap
   * @param isPreformatted Whether the content is preformatted
   * @returns Complete HTML document
   */
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

  /**
   * Injects theme styles into existing HTML content.
   * @param html The HTML content to inject styles into
   * @returns HTML content with injected theme styles
   */
  private injectThemeStyles(html: string): string {
    const themeStyles = this.getThemeAwareStyles();
    
    if (html.includes('<head>')) {
      return html.replace('<head>', `<head>${themeStyles}`);
    } else {
      // If no head tag, wrap the content
      return this.createStyledDocument(html, false);
    }
  }

  /**
   * Gets theme-aware CSS styles for email content rendering.
   * @returns CSS styles as a string
   */
  private getThemeAwareStyles(): string {
    // Use the ThemeService to detect theme
    const isDarkMode = this.themeService.isDarkTheme();
    
    const textColor = isDarkMode ? '#ffffff' : '#333333';
    const bgColor = isDarkMode ? '#1f2937' : '#ffffff';
    const linkColor = isDarkMode ? '#60a5fa' : '#0066cc';
    
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

  /**
   * Escapes HTML special characters for safe display.
   * @param unsafe The unsafe HTML string
   * @returns Escaped HTML string
   */
  private escapeHtml(unsafe: string): string {
    return unsafe
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }
}
