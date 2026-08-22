// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import { TestBed } from '@angular/core/testing';
import { ContentSanitizerService } from './content-sanitizer.service';

describe('ContentSanitizerService', () => {
  let service: ContentSanitizerService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ContentSanitizerService]
    });
    service = TestBed.inject(ContentSanitizerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('stripHtmlTags', () => {
    it('should return empty string for null/undefined input', () => {
      expect(service.stripHtmlTags('')).toBe('');
      expect(service.stripHtmlTags(null as any)).toBe('');
      expect(service.stripHtmlTags(undefined as any)).toBe('');
    });

    it('should remove HTML tags while preserving content', () => {
      const html = '<p>Hello <strong>World</strong>!</p>';
      const result = service.stripHtmlTags(html);
      expect(result).toBe('Hello World!');
    });

    it('should convert br tags to newlines', () => {
      const html = 'Line 1<br>Line 2<br/>Line 3';
      const result = service.stripHtmlTags(html);
      expect(result).toBe('Line 1\nLine 2\nLine 3');
    });

    it('should convert p tags to double newlines', () => {
      const html = '<p>Paragraph 1</p><p>Paragraph 2</p>';
      const result = service.stripHtmlTags(html);
      expect(result).toBe('Paragraph 1\n\nParagraph 2');
    });

    it('should decode HTML entities', () => {
      const html = '&amp; &lt; &gt; &quot; &#039; &nbsp;';
      const result = service.stripHtmlTags(html);
      expect(result).toBe('& < > " \'');
    });

    it('should trim whitespace', () => {
      const html = '  <p>Content</p>  ';
      const result = service.stripHtmlTags(html);
      expect(result).toBe('Content');
    });
  });

  describe('sanitizeHtml', () => {
    it('should return empty string for null/undefined input', () => {
      expect(service.sanitizeHtml('')).toBe('');
      expect(service.sanitizeHtml(null as any)).toBe('');
      expect(service.sanitizeHtml(undefined as any)).toBe('');
    });

    it('should remove script tags', () => {
      const html = '<p>Content</p><script>alert("xss")</script>';
      const result = service.sanitizeHtml(html);
      expect(result).toBe('<p>Content</p>');
    });

    it('should remove iframe tags', () => {
      const html = '<p>Content</p><iframe src="malicious.com"></iframe>';
      const result = service.sanitizeHtml(html);
      expect(result).toBe('<p>Content</p>');
    });

    it('should remove event handlers', () => {
      const html = '<p onclick="alert()" onload="evil()">Content</p>';
      const result = service.sanitizeHtml(html);
      expect(result).toBe('<p>Content</p>');
    });

    it('should remove javascript protocol', () => {
      const html = '<a href="javascript:alert()">Link</a>';
      const result = service.sanitizeHtml(html);
      expect(result).toBe('<a href="">Link</a>');
    });

    it('should preserve safe HTML', () => {
      const html = '<p>Safe <strong>content</strong> with <em>formatting</em></p>';
      const result = service.sanitizeHtml(html);
      expect(result).toBe('<p>Safe <strong>content</strong> with <em>formatting</em></p>');
    });
  });

  describe('extractTextContent', () => {
    it('should return empty string for null/undefined input', () => {
      expect(service.extractTextContent('')).toBe('');
      expect(service.extractTextContent(null as any)).toBe('');
      expect(service.extractTextContent(undefined as any)).toBe('');
    });

    it('should return plain text as-is when isHtml is false', () => {
      const text = 'Plain text content';
      const result = service.extractTextContent(text, false);
      expect(result).toBe('Plain text content');
    });

    it('should strip HTML tags when isHtml is true', () => {
      const html = '<p>HTML <strong>content</strong></p>';
      const result = service.extractTextContent(html, true);
      expect(result).toBe('HTML content');
    });

    it('should trim whitespace', () => {
      const text = '  Content  ';
      const result = service.extractTextContent(text, false);
      expect(result).toBe('Content');
    });
  });

  describe('truncateText', () => {
    it('should return original text if shorter than max length', () => {
      const text = 'Short text';
      const result = service.truncateText(text, 20);
      expect(result).toBe('Short text');
    });

    it('should truncate text and add suffix', () => {
      const text = 'This is a very long text that needs to be truncated';
      const result = service.truncateText(text, 20);
      expect(result).toBe('This is a very lo...');
    });

    it('should use custom suffix', () => {
      const text = 'Long text to truncate';
      const result = service.truncateText(text, 10, '***');
      expect(result).toBe('Long te***');
    });

    it('should handle empty string', () => {
      const result = service.truncateText('', 10);
      expect(result).toBe('');
    });
  });

  describe('escapeHtml', () => {
    it('should return empty string for null/undefined input', () => {
      expect(service.escapeHtml('')).toBe('');
      expect(service.escapeHtml(null as any)).toBe('');
      expect(service.escapeHtml(undefined as any)).toBe('');
    });

    it('should escape HTML special characters', () => {
      const text = '<script>&"\'</script>';
      const result = service.escapeHtml(text);
      expect(result).toBe('&lt;script&gt;&amp;&quot;&#039;&lt;/script&gt;');
    });

    it('should handle text without special characters', () => {
      const text = 'Plain text content';
      const result = service.escapeHtml(text);
      expect(result).toBe('Plain text content');
    });
  });
});
