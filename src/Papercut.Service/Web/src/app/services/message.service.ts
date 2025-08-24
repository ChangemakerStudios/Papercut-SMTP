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
import { Observable, map } from 'rxjs';
import { 
  GetMessagesResponse, 
  DetailDto, 
  RefDto 
} from '../models';
import { MessageApiService } from './message-api.service';
import { ContentFormattingService } from './content-formatting.service';
import { ContentTransformationService } from './content-transformation.service';

/**
 * Service for managing email messages.
 * Provides methods to interact with the Papercut message API.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageService {

  constructor(
    private messageApiService: MessageApiService,
    private contentFormattingService: ContentFormattingService,
    private contentTransformationService: ContentTransformationService
  ) {}

  /**
   * Gets a paginated list of messages.
   * @param limit The maximum number of messages to return (default: 10)
   * @param start The starting index for pagination (default: 0)
   * @returns Observable of GetMessagesResponse
   */
  getMessages(limit: number = 10, start: number = 0): Observable<GetMessagesResponse> {
    return this.messageApiService.getMessages(limit, start);
  }

  /**
   * Gets the basic RefDto information for a specific message.
   * This is faster than getMessage() as it queries the list endpoint.
   * @param messageId The unique message ID
   * @returns Observable of RefDto or null if not found
   */
  getMessageRef(messageId: string): Observable<RefDto | null> {
    return this.messageApiService.getMessageRef(messageId);
  }

  /**
   * Gets the detailed information for a specific message.
   * @param messageId The unique message ID
   * @returns Observable of DetailDto
   */
  getMessage(messageId: string): Observable<DetailDto> {
    return this.messageApiService.getMessageDetail(messageId);
  }

  /**
   * Downloads the raw message file.
   * @param messageId The unique message ID
   */
  downloadRawMessage(messageId: string): void {
    this.messageApiService.downloadRawMessage(messageId);
  }

  /**
   * Downloads a specific message section by index.
   * @param messageId The unique message ID
   * @param sectionIndex The zero-based section index
   */
  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    this.messageApiService.downloadSectionByIndex(messageId, sectionIndex);
  }

  /**
   * Downloads a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   */
  downloadSectionByContentId(messageId: string, contentId: string): void {
    this.messageApiService.downloadSectionByContentId(messageId, contentId);
  }

  /**
   * Downloads raw message file with progress tracking.
   * @param messageId The unique message ID
   */
  downloadRawMessageWithProgress(messageId: string): void {
    this.messageApiService.downloadRawMessageWithProgress(messageId);
  }

  /**
   * Gets the content of a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   * @returns Observable of the section content as text
   */
  getSectionContent(messageId: string, contentId: string): Observable<string> {
    return this.messageApiService.getSectionContent(messageId, contentId);
  }

  /**
   * Gets the content of a specific message section by index.
   * @param messageId The unique message ID
   * @param index The index of the section in the sections array
   * @returns Observable of the section content as text
   */
  getSectionByIndex(messageId: string, index: number): Observable<string> {
    return this.messageApiService.getSectionByIndex(messageId, index);
  }

  /**
   * Gets the raw message content.
   * @param messageId The unique message ID
   * @returns Observable of the raw message content as text
   */
  getRawContent(messageId: string): Observable<string> {
    return this.messageApiService.getRawContent(messageId);
  }

  /**
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.messageApiService.deleteAllMessages();
  }

  // Returns the HTML content for a message, handling plain text and HTML bodies
  getMessageContent(message: DetailDto): string {
    return this.contentFormattingService.getMessageContent(
      message.htmlBody ?? null, 
      message.textBody ?? null, 
      message.id ?? ''
    );
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
    // Apply content transformations first
    let processedContent = content;
    if (messageId && mediaType.toLowerCase() === 'text/html') {
      processedContent = this.contentTransformationService.transformContent(content, messageId);
    }
    
    // Then format the content
    return this.contentFormattingService.formatMessageContent(processedContent, mediaType, messageId);
  }

  /**
   * Formats section content for display in iframe with proper styling and theme support.
   * @param content The raw content
   * @param mediaType The media type of the content
   * @param messageId The message ID for CID reference transformation
   * @returns Formatted HTML content ready for iframe display
   */
  formatSectionContent(content: string, mediaType: string, messageId: string): string {
    return this.contentFormattingService.formatSectionContent(content, mediaType, messageId);
  }
} 