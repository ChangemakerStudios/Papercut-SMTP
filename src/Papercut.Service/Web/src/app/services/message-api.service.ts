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
import { Observable, map, tap } from 'rxjs';
import { 
  GetMessagesResponse, 
  DetailDto, 
  RefDto,
  PaginationOptions
} from '../models';
import { EnvironmentService } from './environment.service';
import { LoggingService } from './logging.service';

/**
 * Unified service for handling message API operations.
 * Combines HTTP communication, data transformation, and business logic.
 * This service provides a clean interface for all message-related operations.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageApiService {
  private readonly baseUrl: string;

  constructor(
    private http: HttpClient,
    private environmentService: EnvironmentService,
    private loggingService: LoggingService
  ) {
    this.baseUrl = this.environmentService.getApiEndpoint('messages');
  }

  /**
   * Gets a paginated list of messages with proper date conversion.
   * @param limit The maximum number of messages to return (default: 10)
   * @param start The starting index for pagination (default: 0)
   * @returns Observable of GetMessagesResponse with converted dates
   */
  getMessages(limit: number = 10, start: number = 0): Observable<GetMessagesResponse> {
    let params = new HttpParams();
    
    if (limit !== undefined) {
      params = params.set('limit', limit.toString());
    }
    if (start !== undefined) {
      params = params.set('start', start.toString());
    }
    
    return this.http.get<GetMessagesResponse>(this.baseUrl, { params }).pipe(
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
    this.loggingService.debug('MessageApiService - Fetching message', { originalId: messageId });
    // The ID from the route parameter is already decoded by Angular router
    const encodedId = encodeURIComponent(messageId);
    const finalUrl = `${this.baseUrl}/${encodedId}`;
    this.loggingService.debug('MessageApiService - Final URL', { url: finalUrl });
    
    const start = performance.now();
    return this.http.get<DetailDto>(finalUrl).pipe(
      map(detail => ({
        ...detail,
        createdAt: detail.createdAt ? new Date(detail.createdAt) : null
      })),
      tap({
        next: (result) => {
          const duration = performance.now() - start;
          this.loggingService.debug('MessageApiService - HTTP call successful');
          this.loggingService.logPerformance('getMessage', duration);
          this.loggingService.logApiCall('GET', finalUrl, 200, duration);
        },
        error: (error) => {
          const duration = performance.now() - start;
          this.loggingService.error('MessageApiService - HTTP call failed', error);
          this.loggingService.logApiCall('GET', finalUrl, error.status, duration);
        }
      })
    );
  }

  /**
   * Gets the raw message content as text.
   * @param messageId The unique message ID
   * @returns Observable of the raw message content as text
   */
  getRawContent(messageId: string): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    return this.http.get(`${this.baseUrl}/${encodedId}/raw`, { responseType: 'text' });
  }

  /**
   * Gets the content of a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   * @returns Observable of the section content as text
   */
  getSectionContent(messageId: string, contentId: string): Observable<string> {
    const encodedMessageId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    return this.http.get(`${this.baseUrl}/${encodedMessageId}/contents/${encodedContentId}`, { responseType: 'text' });
  }

  /**
   * Gets the content of a specific message section by index.
   * @param messageId The unique message ID
   * @param index The index of the section in the sections array
   * @returns Observable of the section content as text
   */
  getSectionByIndex(messageId: string, index: number): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    return this.http.get(`${this.baseUrl}/${encodedId}/sections/${index}`, { responseType: 'text' });
  }

  /**
   * Downloads the raw message file by opening it in a new tab.
   * @param messageId The unique message ID
   */
  downloadRawMessage(messageId: string): void {
    const encodedId = encodeURIComponent(messageId);
    window.open(`${this.baseUrl}/${encodedId}/raw`, '_blank');
  }

  /**
   * Downloads a specific message section by index.
   * @param messageId The unique message ID
   * @param sectionIndex The zero-based section index
   */
  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    const encodedId = encodeURIComponent(messageId);
    window.open(`${this.baseUrl}/${encodedId}/sections/${sectionIndex}`, '_blank');
  }

  /**
   * Downloads a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   */
  downloadSectionByContentId(messageId: string, contentId: string): void {
    const encodedMessageId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    window.open(`${this.baseUrl}/${encodedMessageId}/contents/${encodedContentId}`, '_blank');
  }

  /**
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }

  /**
   * Downloads raw message file with progress tracking.
   * Note: This currently uses the basic download method.
   * Future enhancement could integrate with a FileDownloaderService for progress tracking.
   * @param messageId The unique message ID
   */
  downloadRawMessageWithProgress(messageId: string): void {
    // This will be enhanced when FileDownloaderService is implemented
    // For now, use the basic download method as fallback
    this.downloadRawMessage(messageId);
  }
}
