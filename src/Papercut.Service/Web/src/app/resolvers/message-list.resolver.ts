import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { MessageRepository, MessageResponse } from '../services/message.repository';

@Injectable({
  providedIn: 'root'
})
export class MessageListResolver implements Resolve<MessageResponse> {
  constructor(private messageRepository: MessageRepository) {}

  resolve(route: ActivatedRouteSnapshot): Observable<MessageResponse> {
    const limit = parseInt(route.queryParams['limit'] || '10', 10);
    const start = parseInt(route.queryParams['start'] || '0', 10);
    
    return this.messageRepository.getMessages({ limit, start });
  }
} 