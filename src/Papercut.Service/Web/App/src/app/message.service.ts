import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import { Observable } from 'rxjs';
import {environment} from "../environments/environment";

export interface Message {
  size: number;
  id: string;
  createdAt: Date;
  subject: string;
}

export interface MessageResponse {
  totalMessageCount: number;
  messages: Message[];
}

@Injectable({
  providedIn: 'root',
  deps: [HttpClient]
})
export class MessageService {

  constructor(private httpClient: HttpClient) { }
  
  public getMessages() : Observable<MessageResponse> {
    return this.httpClient.get<MessageResponse>("/api/Messages");
  }
}
