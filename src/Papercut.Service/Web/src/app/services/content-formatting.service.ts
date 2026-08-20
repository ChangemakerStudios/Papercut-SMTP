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
      return this.createStyledDocument(`<pre class="papercut-plain-text">${escapedContent}</pre>`, true);
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
  /**
   * Wraps service-rendered html (already sanitized, with inline content
   * resolved) in the theme styles and document head. No client-side
   * transformation is needed -- the service did that work against the real
   * MIME tree.
   */
  styleRenderedHtml(html: string): string {
    if (!html) return this.createStyledDocument('Loading...', true);

    // plain-text bodies carry a marker class and get themed; designed email
    // html is left to render in its own colors
    return this.injectThemeStyles(html, html.includes('papercut-plain-text'));
  }

  /**
   * The base href makes the service's relative inline-content URLs
   * (api/messages/.../contents/...) resolve against the app root rather than
   * the current route, which also keeps them working under an HttpPathPrefix.
   */
  private getDocumentHead(): string {
    return `<meta name="referrer" content="no-referrer"><base href="${document.baseURI}" target="_blank">`;
  }

  private createStyledDocument(content: string, isPreformatted: boolean): string {
    // preformatted content is plain text (no design of its own) -- theme it
    const themeStyles = this.getThemeAwareStyles(isPreformatted);
    const bodyContent = isPreformatted ? content : content;
    
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        ${this.getDocumentHead()}
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
  private injectThemeStyles(html: string, themed = false): string {
    const themeStyles = this.getThemeAwareStyles(themed);

    // match <head>, <HEAD> and <head lang="en"> alike -- a missed match used to
    // fall through and nest a whole document inside another one
    const headTag = /<head\b[^>]*>/i;

    if (headTag.test(html)) {
      // base/referrer go in with the styles so links in full email documents
      // open in a new tab too (the host also intercepts clicks -- see SafeIframeComponent)
      return html.replace(headTag, match => `${match}${this.getDocumentHead()}${themeStyles}`);
    } else {
      // If no head tag, wrap the content -- keeping the themed decision, since
      // plain-text bodies (a bare <pre>) always take this path
      return this.createStyledDocument(html, themed);
    }
  }

  /**
   * Gets theme-aware CSS styles for email content rendering.
   * @returns CSS styles as a string
   */
  private getThemeAwareStyles(themed = false): string {
    // Designed email HTML is rendered as the recipient would see it -- the
    // desktop preview injects no colors at all, and forcing them here used to
    // flatten every branded email (it even put borders and padding on every
    // table cell, which wrecks the table layouts most emails are built from).
    // Only the plain-text view, which has no design of its own, gets themed.
    const shared = `
      /* long unbroken strings must not blow out the layout (issue #154 --
         the desktop applies this through HtmlToHtmlFormatWrapper) */
      * {
        overflow-wrap: break-word;
        word-wrap: break-word;
      }
      img {
        max-width: 100%;
        height: auto;
      }`;

    if (!themed) {
      // Email HTML is authored for a white canvas, so give it one whatever the
      // app theme is -- exactly what the desktop's WebView2 does. Without a
      // background the frame would show the dark app surface behind dark text.
      // These are plain declarations, so an email's own background/color wins.
      return `<style>
      html, body {
        background: #ffffff;
        color: #1a1a1a;
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      body {
        padding: 4px;
      }
      ${shared}
    </style>`;
    }

    const tokens = getComputedStyle(document.body);
    const isDarkMode = this.themeService.isDarkTheme();
    const textColor = tokens.getPropertyValue('--pc-ink').trim() || (isDarkMode ? '#d6dde6' : '#2d3748');
    const bgColor = tokens.getPropertyValue('--pc-surface').trim() || (isDarkMode ? '#1a202b' : '#ffffff');
    const monoFont = tokens.getPropertyValue('--pc-font-mono').trim() || `'Courier New', monospace`;

    return `<style>
      html, body {
        background: ${bgColor};
        color: ${textColor};
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      body {
        padding: 4px;
      }
      pre {
        font-family: ${monoFont};
        font-size: 12px;
        line-height: 1.5em;
        white-space: pre-wrap;
        margin: 0;
        padding: 0;
        color: ${textColor};
      }
      ${shared}
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
