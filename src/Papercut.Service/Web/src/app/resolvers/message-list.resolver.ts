import { Injectable } from '@angular/core';
import { Resolve } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

interface Message {
  id: string;
  subject: string;
  size: string;
  createdAt: string;
}

interface MessageResponse {
  totalMessageCount: number;
  messages: Message[];
}

@Injectable({
  providedIn: 'root'
})
export class MessageListResolver implements Resolve<MessageResponse> {
  constructor(private http: HttpClient) {}

  resolve(): Observable<MessageResponse> {
    return this.http.get<MessageResponse>('/api/messages');
  }
} 