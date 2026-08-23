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
 * Service for transforming email content references and URLs.
 * Handles CID reference transformations and URL absolute conversions.
 * This service focuses solely on content transformation operations.
 */
@Injectable({
  providedIn: 'root'
})
export class ContentTransformationService {

  constructor() {}

  /**
   * Transforms CID references in HTML content to proper API URLs.
   * @param html The HTML content containing CID references
   * @param messageId The message ID for constructing API URLs
   * @returns HTML content with transformed CID references
   */
  transformCidReferences(html: string, messageId: string): string {
    if (!html || !messageId) {
      return html;
    }
    
    return html.replace(/src=["']cid:([^"']+)["']/gi, (match, contentId) => {
      const encodedMessageId = encodeURIComponent(messageId);
      const encodedContentId = encodeURIComponent(contentId);
      return `src="/api/messages/${encodedMessageId}/contents/${encodedContentId}"`;
    });
  }

  /**
   * Converts relative API URLs to absolute URLs.
   * @param html The HTML content containing relative URLs
   * @returns HTML content with absolute URLs
   */
  makeUrlsAbsolute(html: string): string {
    if (!html) return html;
    
    const baseUrl = window.location.origin;
    return html.replace(/src=["']\/api\/([^"']+)["']/gi, (match, apiPath) => {
      return `src="${baseUrl}/api/${apiPath}"`;
    });
  }

  /**
   * Applies all content transformations to HTML content.
   * @param html The HTML content to transform
   * @param messageId The message ID for CID transformations
   * @returns Fully transformed HTML content
   */
  transformContent(html: string, messageId: string): string {
    if (!html || !messageId) {
      return html;
    }

    let transformedContent = html;
    
    // Apply CID reference transformations
    transformedContent = this.transformCidReferences(transformedContent, messageId);
    
    // Apply URL absolute conversions
    transformedContent = this.makeUrlsAbsolute(transformedContent);
    
    return transformedContent;
  }
}
