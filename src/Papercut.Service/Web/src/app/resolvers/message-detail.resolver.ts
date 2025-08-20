import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { MessageRepository, MessageDetail } from '../services/message.repository';
import { LoggingService } from '../services/logging.service';

@Injectable({
  providedIn: 'root'
})
export class MessageDetailResolver implements Resolve<MessageDetail> {
  constructor(
    private messageRepository: MessageRepository,
    private loggingService: LoggingService
  ) {}

  resolve(route: ActivatedRouteSnapshot): Observable<MessageDetail> {
    const messageId = route.paramMap.get('id');
    this.loggingService.debug('MessageDetailResolver - Raw message ID from route', { messageId });
    
    if (!messageId) {
      throw new Error('Message ID is required');
    }
    
    // Decode the message ID since it comes URL encoded from the route
    const decodedId = decodeURIComponent(messageId);
    this.loggingService.debug('MessageDetailResolver - Decoded message ID', { decodedId });
    
    return this.messageRepository.getMessage(decodedId).pipe(
      tap({
        next: (result) => this.loggingService.debug('MessageDetailResolver - API call successful', { messageId: result.id }),
        error: (error) => this.loggingService.error('MessageDetailResolver - API call failed', error)
      })
    );
  }
} 