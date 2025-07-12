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
   * Gets the detailed information for a specific message.
   * @param messageId The unique message ID
   * @returns Observable of DetailDto
   */
  getMessage(messageId: string): Observable<DetailDto> {
    return this.http.get<DetailDto>(`${this.apiUrl}/${messageId}`)
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
    const url = `${this.apiUrl}/${messageId}/raw`;
    window.open(url, '_blank');
  }

  /**
   * Downloads a specific message section by index.
   * @param messageId The unique message ID
   * @param sectionIndex The zero-based section index
   */
  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    const url = `${this.apiUrl}/${messageId}/sections/${sectionIndex}`;
    window.open(url, '_blank');
  }

  /**
   * Downloads a specific message section by content ID.
   * @param messageId The unique message ID
   * @param contentId The content ID of the section
   */
  downloadSectionByContentId(messageId: string, contentId: string): void {
    const url = `${this.apiUrl}/${messageId}/contents/${contentId}`;
    window.open(url, '_blank');
  }

  /**
   * Deletes all messages.
   * @returns Observable of void
   */
  deleteAllMessages(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
} 