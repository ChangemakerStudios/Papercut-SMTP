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

/**
 * Service for sanitizing and processing content for safe display.
 * Handles HTML stripping, content cleaning, and security concerns.
 * This service focuses solely on content sanitization operations.
 */
@Injectable({
  providedIn: 'root'
})
export class ContentSanitizerService {

  constructor() {}

  /**
   * Strips HTML tags from content to extract plain text.
   * @param html The HTML content to strip tags from
   * @returns Plain text content with HTML tags removed
   */
  stripHtmlTags(html: string): string {
    if (!html) return '';
    
    // Remove HTML tags while preserving line breaks and spacing
    let result = html
      .replace(/<br\s*\/?>/gi, '\n')  // Convert <br> tags to newlines
      .replace(/<\/p>/gi, '\n\n')     // Convert </p> tags to double newlines
      .replace(/<[^>]*>/g, '')        // Remove all remaining HTML tags
      .replace(/&nbsp;/g, '\uE000')   // Convert &nbsp; to private use character (temporary)
      .replace(/&amp;/g, '&')         // Convert &amp; to &
      .replace(/&lt;/g, '<')          // Convert &lt; to <
      .replace(/&gt;/g, '>')          // Convert &gt; to >
      .replace(/&quot;/g, '"')        // Convert &quot; to "
      .replace(/&#039;/g, "'")        // Convert &#039; to '
      .replace(/[ \t]+/g, ' ')        // Normalize multiple spaces/tabs to single space (preserve newlines)
      .trim();                        // Remove leading/trailing whitespace
    
    // Convert private use character back to space (preserves meaningful spaces after trim)
    return result.replace(/\uE000/g, ' ').trim();
  }

  /**
   * Sanitizes HTML content for safe display.
   * @param html The HTML content to sanitize
   * @returns Sanitized HTML content
   */
  sanitizeHtml(html: string): string {
    if (!html) return '';
    
    // Basic HTML sanitization - remove potentially dangerous tags and attributes
    return html
      .replace(/<script[^>]*>.*?<\/script>/gi, '')  // Remove script tags
      .replace(/<iframe[^>]*>.*?<\/iframe>/gi, '')   // Remove iframe tags
      .replace(/<object[^>]*>.*?<\/object>/gi, '')   // Remove object tags
      .replace(/<embed[^>]*>.*?<\/embed>/gi, '')      // Remove embed tags
      .replace(/\s*on\w+\s*=\s*["'][^"']*["']/gi, '')  // Remove event handlers
      .replace(/javascript:[^"'\s>]*/gi, '')                                // Remove javascript: protocol
      .replace(/vbscript:/gi, '')                                           // Remove vbscript: protocol
      .replace(/data:/gi, '')                                               // Remove data: protocol
      .trim();
  }

  /**
   * Extracts plain text content from HTML or text content.
   * @param content The content to extract text from
   * @param isHtml Whether the content is HTML (default: false)
   * @returns Plain text content
   */
  extractTextContent(content: string, isHtml: boolean = false): string {
    if (!content) return '';
    
    if (isHtml) {
      return this.stripHtmlTags(content);
    }
    
    return content.trim();
  }

  /**
   * Truncates text content to a specified length.
   * @param text The text to truncate
   * @param maxLength Maximum length before truncation
   * @param suffix Suffix to add when truncated (default: '...')
   * @returns Truncated text
   */
  truncateText(text: string, maxLength: number, suffix: string = '...'): string {
    if (!text || text.length <= maxLength) return text;
    
    const availableLength = maxLength - suffix.length;
    if (availableLength <= 0) return suffix;
    
    return text.substring(0, availableLength) + suffix;
  }

  /**
   * Escapes HTML special characters for safe display.
   * @param text The text to escape
   * @returns Escaped HTML text
   */
  escapeHtml(text: string): string {
    if (!text) return '';
    
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }
}
