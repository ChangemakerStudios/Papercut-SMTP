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

import { Pipe, PipeTransform } from '@angular/core';
import { EmailAddressDto } from '../models';
import { EmailService } from '../services/email.service';

/**
 * Pipe for formatting an array of email addresses into a readable string.
 */
@Pipe({
  name: 'emailList',
  standalone: true
})
export class EmailListPipe implements PipeTransform {
  
  constructor(private emailService: EmailService) {}

  transform(emailAddresses: EmailAddressDto[] | null | undefined): string {
    if (!emailAddresses || emailAddresses.length === 0) {
      return '';
    }

    return this.emailService.formatEmailAddressList(emailAddresses);
  }
} 