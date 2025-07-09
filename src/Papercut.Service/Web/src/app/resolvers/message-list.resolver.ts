import { Injectable } from '@angular/core';
import { Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { MessageRepository, MessageResponse } from '../services/message.repository';

@Injectable({
  providedIn: 'root'
})
export class MessageListResolver implements Resolve<MessageResponse> {
  constructor(private messageRepository: MessageRepository) {}

  resolve(): Observable<MessageResponse> {
    return this.messageRepository.getMessages();
  }
} 