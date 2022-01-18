import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MessageItem {
  size: string;
  id: string;
  createdAt?: Date;
  subject: string;
}

export interface GetMessagesResponse {
  totalMessageCount: number;
  messages: MessageItem[];
}

@Injectable({
  providedIn: 'root'
})
export class MessageRepositoryService {

  constructor(private http: HttpClient) { }

  findOne(id: string): Observable<MessageItem> {
    return this.http.get<MessageItem>(`/api/messages/${id}`);
  }

  list(limit?: number, skip?: number): Observable<GetMessagesResponse> {
    return this.http.get<GetMessagesResponse>('/api/messages');
  }
}
