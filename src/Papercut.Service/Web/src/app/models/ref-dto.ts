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

import { EmailAddressDto } from "./email-address-dto";

/**
 * Represents a message reference with basic metadata.
 * Used for message list views. Matches the C# RefDto class.
 */
export interface RefDto {  
  /** The unique message ID */
  id?: string | null;
  
  /** The creation timestamp */
  createdAt?: Date | null;
  
  /** The message subject */
  subject?: string | null;

  /** The file size as a string */
  size: number;

  from?: EmailAddressDto[] | null;
} 