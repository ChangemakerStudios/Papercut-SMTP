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
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ContentTransformationService } from '../services/content-transformation.service';

/**
 * Pipe for transforming CID references in email HTML content to proper API URLs.
 */
@Pipe({
  name: 'cidTransform',
  standalone: true
})
export class CidTransformPipe implements PipeTransform {
  constructor(
    private sanitizer: DomSanitizer,
    private contentTransformationService: ContentTransformationService
  ) {}

  transform(html: string | null | undefined, messageId: string): SafeHtml {
    if (!html) {
      return this.sanitizer.bypassSecurityTrustHtml('');
    }

    // Transform CID references to API URLs using the service
    const transformedHtml = this.contentTransformationService.transformCidReferences(html, messageId);
    
    // Return sanitized HTML
    return this.sanitizer.bypassSecurityTrustHtml(transformedHtml);
  }
} 