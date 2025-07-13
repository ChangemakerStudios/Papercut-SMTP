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

import { EmailAddressDto } from './email-address-dto';
import { HeaderDto } from './header-dto';
import { EmailAttachmentDto } from './email-attachment-dto';

/**
 * Represents the complete details of an email message.
 * Matches the C# DetailDto class.
 */
export interface DetailDto {
  /** The unique message ID */
  id?: string | null;

  /** The message name */
  name?: string | null;
  
  /** The creation timestamp */
  createdAt?: Date | null;
  
  /** The message subject */
  subject?: string | null;
  
  /** The sender addresses */
  from: EmailAddressDto[];
  
  /** The recipient addresses */
  to: EmailAddressDto[];
  
  /** The CC addresses */
  cc: EmailAddressDto[];
  
  /** The BCC addresses */
  bCc: EmailAddressDto[];
  
  /** The HTML body content */
  htmlBody?: string | null;
  
  /** The plain text body content */
  textBody?: string | null;
  
  /** The message headers */
  headers: HeaderDto[];
  
  /** The message sections/attachments */
  sections: EmailAttachmentDto[];
} 