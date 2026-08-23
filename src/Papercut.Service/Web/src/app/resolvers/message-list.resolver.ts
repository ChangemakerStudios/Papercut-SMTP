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
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { MessageApiService } from '../services/message-api.service';
import { GetMessagesResponse } from '../models';

/**
 * Resolver for loading the message list.
 * Preloads messages before the component is displayed.
 */
@Injectable({
  providedIn: 'root'
})
export class MessageListResolver implements Resolve<GetMessagesResponse> {
  constructor(private messageApiService: MessageApiService) {}

  resolve(
    route: ActivatedRouteSnapshot, 
    state: RouterStateSnapshot
  ): Observable<GetMessagesResponse> {
    const limit = parseInt(route.queryParams['limit'] || '10', 10);
    const start = parseInt(route.queryParams['start'] || '0', 10);
    
    return this.messageApiService.getMessages(limit, start);
  }
} 