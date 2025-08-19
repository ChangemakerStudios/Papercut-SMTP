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
  RefDto,
  PaginationOptions
} from '../models';
import { MessageRepository } from './message.repository';

/**
 * Service for handling message API operations.
 * Provides a clean interface for HTTP operations related to messages.
 * This service focuses solely on API communication and data retrieval.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageApiService {

  constructor(private messageRepository: MessageRepository) {}

  /**
   * Gets a paginated list of messages with proper date conversion.
   * @param limit The maximum number of messages to return (default: 10)
   * @param start The starting index for pagination (default: 0)
   * @returns Observable of GetMessagesResponse with converted dates
   */
  getMessages(limit: number = 10, start: number = 0): Observable<GetMessagesResponse> {
    return this.messageRepository.getMessages({ limit, start })
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
   * This is faster than getMessageDetail() as it queries the list endpoint.
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
   * Gets the detailed information for a specific message with proper date conversion.
   * @param messageId The unique message ID
   * @returns Observable of DetailDto with converted dates
   */
  getMessageDetail(messageId: string): Observable<DetailDto> {
    return this.messageRepository.getMessage(messageId)
      .pipe(
        map(detail => ({
          ...detail,
          createdAt: detail.createdAt ? new Date(detail.createdAt) : null
        }))
      );
  }

  /**
   * Gets the raw message content as text.
   * @param messageId The unique message ID
   * @returns Observable of the raw message content as text
   */
  getRawContent(messageId: string): Observable<string> {
    return this.messageRepository.getRawContent(messageId);
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
   * Downloads the raw message file by opening it in a new tab.
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
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.messageRepository.deleteAllMessages();
  }

  /**
   * Downloads raw message file with progress tracking.
   * Note: This currently uses the basic download method.
   * Future enhancement could integrate with a FileDownloaderService for progress tracking.
   * @param messageId The unique message ID
   */
  downloadRawMessageWithProgress(messageId: string): void {
    // This will be enhanced when FileDownloaderService is implemented
    // For now, use the repository method as fallback
    this.messageRepository.downloadRawMessage(messageId);
  }
}
