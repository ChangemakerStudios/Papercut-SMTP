import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface EmailAddress {
  name: string;
  address: string;
}

export interface Header {
  name: string;
  value: string;
}

export interface Section {
  id: string | null;
  mediaType: string;
  fileName: string | null;
}

export interface Message {
  id: string;
  subject: string;
  size: string;
  createdAt: string;
  from: EmailAddress[];
}

export interface MessageDetail {
  id: string;
  createdAt: string;
  subject: string;
  from: EmailAddress[];
  to: EmailAddress[];
  cc: EmailAddress[];
  bCc: EmailAddress[];
  htmlBody: string;
  textBody: string;
  headers: Header[];
  sections: Section[];
}

export interface MessageResponse {
  totalMessageCount: number;
  messages: Message[];
}

export interface PaginationOptions {
  limit?: number;
  start?: number;
}

@Injectable({
  providedIn: 'root'
})
export class MessageRepository {
  private readonly baseUrl = '/api/messages';

  constructor(private http: HttpClient) {}

  getMessages(options?: PaginationOptions): Observable<MessageResponse> {
    let params = new HttpParams();
    
    if (options) {
      if (options.limit) {
        params = params.set('limit', options.limit.toString());
      }
      if (options.start) {
        params = params.set('start', options.start.toString());
      }
    }
    
    return this.http.get<MessageResponse>(this.baseUrl, { params });
  }

  getMessage(id: string): Observable<MessageDetail> {
    console.log('MessageRepository - Original ID:', id);
    // The ID from the route parameter is already decoded by Angular router
    const encodedId = encodeURIComponent(id);
    const finalUrl = `${this.baseUrl}/${encodedId}`;
    console.log('MessageRepository - Final URL:', finalUrl);
    
    return this.http.get<MessageDetail>(finalUrl).pipe(
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
} 