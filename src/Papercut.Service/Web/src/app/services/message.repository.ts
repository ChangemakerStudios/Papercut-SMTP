import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { RefDto, DetailDto, GetMessagesResponse, PaginationOptions } from '../models';

@Injectable({
  providedIn: 'root'
})
export class MessageRepository {
  private readonly baseUrl = '/api/messages';

  constructor(private http: HttpClient) {}

  getMessages(options?: PaginationOptions): Observable<GetMessagesResponse> {
    let params = new HttpParams();
    
    if (options) {
      if (options.limit !== undefined) {
        params = params.set('limit', options.limit.toString());
      }
      if (options.start !== undefined) {
        params = params.set('start', options.start.toString());
      }
    }
    
    return this.http.get<GetMessagesResponse>(this.baseUrl, { params });
  }

  getMessage(id: string): Observable<DetailDto> {
    console.log('MessageRepository - Original ID:', id);
    // The ID from the route parameter is already decoded by Angular router
    const encodedId = encodeURIComponent(id);
    const finalUrl = `${this.baseUrl}/${encodedId}`;
    console.log('MessageRepository - Final URL:', finalUrl);
    
    return this.http.get<DetailDto>(finalUrl).pipe(
      tap({
        next: (result) => console.log('MessageRepository - HTTP call successful'),
        error: (error) => console.error('MessageRepository - HTTP call failed:', error)
      })
    );
  }

  downloadRawMessage(messageId: string): void {
    const encodedId = encodeURIComponent(messageId);
    window.open(`${this.baseUrl}/${encodedId}/raw`, '_blank');
  }

  downloadSectionByContentId(messageId: string, contentId: string): void {
    const encodedMessageId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    window.open(`${this.baseUrl}/${encodedMessageId}/contents/${encodedContentId}`, '_blank');
  }

  downloadSectionByIndex(messageId: string, sectionIndex: number): void {
    const encodedId = encodeURIComponent(messageId);
    window.open(`${this.baseUrl}/${encodedId}/sections/${sectionIndex}`, '_blank');
  }

  getRawContent(messageId: string): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    return this.http.get(`${this.baseUrl}/${encodedId}/raw`, { responseType: 'text' });
  }

  getSectionContent(messageId: string, contentId: string): Observable<string> {
    const encodedMessageId = encodeURIComponent(messageId);
    const encodedContentId = encodeURIComponent(contentId);
    return this.http.get(`${this.baseUrl}/${encodedMessageId}/contents/${encodedContentId}`, { responseType: 'text' });
  }

  getSectionByIndex(messageId: string, index: number): Observable<string> {
    const encodedId = encodeURIComponent(messageId);
    return this.http.get(`${this.baseUrl}/${encodedId}/sections/${index}`, { responseType: 'text' });
  }

  deleteAllMessages(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
} 