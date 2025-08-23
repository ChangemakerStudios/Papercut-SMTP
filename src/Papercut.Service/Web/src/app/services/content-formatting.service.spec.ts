import { TestBed } from '@angular/core/testing';
import { ContentFormattingService } from './content-formatting.service';
import { ThemeService } from './theme.service';

describe('ContentFormattingService', () => {
  let service: ContentFormattingService;
  let themeService: jasmine.SpyObj<ThemeService>;

  beforeEach(() => {
    const themeServiceSpy = jasmine.createSpyObj('ThemeService', ['isDarkTheme']);
    
    TestBed.configureTestingModule({
      providers: [
        ContentFormattingService,
        { provide: ThemeService, useValue: themeServiceSpy }
      ]
    });
    
    service = TestBed.inject(ContentFormattingService);
    themeService = TestBed.inject(ThemeService) as jasmine.SpyObj<ThemeService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('formatMessageContent', () => {
    it('should return loading placeholder for empty content', () => {
      const result = service.formatMessageContent('', 'text/html', 'msg123');
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('Loading...');
      expect(result).toContain('<html>');
      expect(result).toContain('</html>');
    });

    it('should format HTML content with complete document', () => {
      const htmlContent = '<html><head></head><body><p>Test content</p></body></html>';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.formatMessageContent(htmlContent, 'text/html', 'msg123');
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<p>Test content</p>');
      expect(result).toContain('color: #333333');
      expect(result).toContain('background: #ffffff');
    });

    it('should format HTML content without complete document', () => {
      const htmlContent = '<p>Test content</p>';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.formatMessageContent(htmlContent, 'text/html', 'msg123');
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<p>Test content</p>');
      expect(result).toContain('<html>');
      expect(result).toContain('</html>');
    });

    it('should format plain text content', () => {
      const textContent = 'Plain text content';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.formatMessageContent(textContent, 'text/plain', 'msg123');
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<pre>Plain text content</pre>');
      expect(result).toContain('font-family: \'Courier New\', monospace');
    });

    it('should apply dark theme styles when theme is dark', () => {
      const htmlContent = '<p>Test content</p>';
      themeService.isDarkTheme.and.returnValue(true);
      
      const result = service.formatMessageContent(htmlContent, 'text/html', 'msg123');
      
      expect(result).toContain('color: #ffffff');
      expect(result).toContain('background: #1f2937');
      expect(result).toContain('color: #60a5fa');
    });

    it('should apply light theme styles when theme is light', () => {
      const htmlContent = '<p>Test content</p>';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.formatMessageContent(htmlContent, 'text/html', 'msg123');
      
      expect(result).toContain('color: #333333');
      expect(result).toContain('background: #ffffff');
      expect(result).toContain('color: #0066cc');
    });
  });

  describe('formatSectionContent', () => {
    it('should delegate to formatMessageContent', () => {
      const content = '<p>Section content</p>';
      const mediaType = 'text/html';
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.formatSectionContent(content, mediaType, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<p>Section content</p>');
    });
  });

  describe('getMessageContent', () => {
    it('should format HTML body when available', () => {
      const htmlBody = '<p>HTML content</p>';
      const textBody = 'Text content';
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<p>HTML content</p>');
    });

    it('should format text body when HTML body is not available', () => {
      const htmlBody = null;
      const textBody = 'Text content';
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<pre>Text content</pre>');
    });

    it('should handle empty content gracefully', () => {
      const htmlBody = null;
      const textBody = null;
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('Loading...');
    });
  });

  describe('parseHtml', () => {
    it('should return HTML content as-is', () => {
      const html = '<p>Test HTML</p>';
      const result = service.parseHtml(html);
      expect(result).toBe(html);
    });

    it('should handle empty HTML', () => {
      const result = service.parseHtml('');
      expect(result).toBe('');
    });
  });

  describe('theme-aware styling', () => {
    it('should include proper CSS reset and base styles', () => {
      themeService.isDarkTheme.and.returnValue(false);
      const result = service.formatMessageContent('<p>Test</p>', 'text/html', 'msg123');
      
      expect(result).toContain('font-family: -apple-system, BlinkMacSystemFont');
      expect(result).toContain('line-height: 1.6');
      expect(result).toContain('margin: 0');
      expect(result).toContain('padding: 4px');
    });

    it('should include table styling', () => {
      themeService.isDarkTheme.and.returnValue(false);
      const result = service.formatMessageContent('<p>Test</p>', 'text/html', 'msg123');
      
      expect(result).toContain('border-collapse: collapse');
      expect(result).toContain('border: 1px solid');
    });

    it('should include image styling', () => {
      themeService.isDarkTheme.and.returnValue(false);
      const result = service.formatMessageContent('<p>Test</p>', 'text/html', 'msg123');
      
      expect(result).toContain('max-width: 100%');
      expect(result).toContain('height: auto');
    });

    it('should include pre styling for text content', () => {
      themeService.isDarkTheme.and.returnValue(false);
      const result = service.formatMessageContent('Text content', 'text/plain', 'msg123');
      
      expect(result).toContain('font-family: \'Courier New\', monospace');
      expect(result).toContain('white-space: pre-wrap');
      expect(result).toContain('word-wrap: break-word');
    });
  });
});
