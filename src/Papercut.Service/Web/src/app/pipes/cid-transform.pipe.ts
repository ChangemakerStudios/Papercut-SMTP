import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'cidTransform',
  standalone: true
})
export class CidTransformPipe implements PipeTransform {
  transform(html: string, messageId: string): string {
    if (!html || !messageId) {
      return html;
    }

    // Replace cid: references with API URLs
    // Matches patterns like: src="cid:image1" or src='cid:content-id'
    return html.replace(/src=["']cid:([^"']+)["']/gi, (match, contentId) => {
      const encodedMessageId = encodeURIComponent(messageId);
      const encodedContentId = encodeURIComponent(contentId);
      return `src="/api/messages/${encodedMessageId}/contents/${encodedContentId}"`;
    });
  }
} 