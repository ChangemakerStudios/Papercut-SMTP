import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { MessageRepository, MessageDetail } from '../services/message.repository';

@Injectable({
  providedIn: 'root'
})
export class MessageDetailResolver implements Resolve<MessageDetail> {
  constructor(private messageRepository: MessageRepository) {}

  resolve(route: ActivatedRouteSnapshot): Observable<MessageDetail> {
    const messageId = route.paramMap.get('id');
    console.log('MessageDetailResolver - Raw message ID from route:', messageId);
    
    if (!messageId) {
      throw new Error('Message ID is required');
    }
    
    // Decode the message ID since it comes URL encoded from the route
    const decodedId = decodeURIComponent(messageId);
    console.log('MessageDetailResolver - Decoded message ID:', decodedId);
    
    return this.messageRepository.getMessage(decodedId).pipe(
      tap({
        next: (result) => console.log('MessageDetailResolver - API call successful:', result),
        error: (error) => console.error('MessageDetailResolver - API call failed:', error)
      })
    );
  }
} 