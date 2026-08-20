import { TestBed } from '@angular/core/testing';
import { ContentFormattingService } from './content-formatting.service';
import { ThemeService } from './theme.service';
import { ContentTransformationService } from './content-transformation.service';

describe('ContentFormattingService', () => {
  let service: ContentFormattingService;
  let themeService: jasmine.SpyObj<ThemeService>;
  let contentTransformationService: jasmine.SpyObj<ContentTransformationService>;

  beforeEach(() => {
    const themeServiceSpy = jasmine.createSpyObj('ThemeService', ['isDarkTheme']);
    const contentTransformationServiceSpy = jasmine.createSpyObj('ContentTransformationService', ['transformContent']);
    
    TestBed.configureTestingModule({
      providers: [
        ContentFormattingService,
        { provide: ThemeService, useValue: themeServiceSpy },
        { provide: ContentTransformationService, useValue: contentTransformationServiceSpy }
      ]
    });
    
    service = TestBed.inject(ContentFormattingService);
    themeService = TestBed.inject(ThemeService) as jasmine.SpyObj<ThemeService>;
    contentTransformationService = TestBed.inject(ContentTransformationService) as jasmine.SpyObj<ContentTransformationService>;
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

      expect(result).toContain('<html>');
      expect(result).toContain('<p>Test content</p>');
    });

    it('should leave designed html to render in its own colors', () => {
      const htmlContent = '<html><head></head><body><p style="color: #ff0000">Branded</p></body></html>';
      themeService.isDarkTheme.and.returnValue(false);

      const result = service.formatMessageContent(htmlContent, 'text/html', 'msg123');

      const tokens = getComputedStyle(document.body);
      const ink = tokens.getPropertyValue('--pc-ink').trim();

      // the email's own styling must survive: no forced colors, and no
      // borders/padding imposed on the table layouts emails are built from
      expect(result).toContain('color: #ff0000');
      expect(result).not.toContain(`color: ${ink} !important`);
      expect(result).not.toContain('border: 1px solid');
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
      expect(result).toContain('Plain text content');
      expect(result).toContain('papercut-plain-text');
    });

    it('should style plain text from the dark token palette when the theme is dark', () => {
      themeService.isDarkTheme.and.returnValue(true);
      document.body.setAttribute('data-theme', 'dark');

      try {
        const result = service.formatMessageContent('Plain text', 'text/plain', 'msg123');

        const tokens = getComputedStyle(document.body);
        expect(result).toContain(`color: ${tokens.getPropertyValue('--pc-ink').trim()}`);
        expect(result).toContain(`background: ${tokens.getPropertyValue('--pc-surface').trim()}`);
      } finally {
        document.body.removeAttribute('data-theme');
      }
    });

    it('should style plain text from the light token palette when the theme is light', () => {
      themeService.isDarkTheme.and.returnValue(false);

      const result = service.formatMessageContent('Plain text', 'text/plain', 'msg123');

      const tokens = getComputedStyle(document.body);
      expect(result).toContain(`color: ${tokens.getPropertyValue('--pc-ink').trim()}`);
      expect(result).toContain(`background: ${tokens.getPropertyValue('--pc-surface').trim()}`);
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
      contentTransformationService.transformContent.and.returnValue(htmlBody);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('<p>HTML content</p>');
      expect(contentTransformationService.transformContent).toHaveBeenCalledWith(htmlBody, messageId);
    });

    it('should format text body when HTML body is not available', () => {
      const htmlBody = null;
      const textBody = 'Text content';
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('Text content');
      expect(contentTransformationService.transformContent).not.toHaveBeenCalled();
    });

    it('should handle empty content gracefully', () => {
      const htmlBody = null;
      const textBody = null;
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('Loading...');
      expect(contentTransformationService.transformContent).not.toHaveBeenCalled();
    });

    it('should apply content transformations for HTML content with message ID', () => {
      const htmlBody = '<img src="cid:image123" alt="test">';
      const textBody = null;
      const messageId = 'msg123';
      const transformedHtml = '<img src="/api/messages/msg123/contents/image123" alt="test">';
      themeService.isDarkTheme.and.returnValue(false);
      contentTransformationService.transformContent.and.returnValue(transformedHtml);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain(transformedHtml);
      expect(contentTransformationService.transformContent).toHaveBeenCalledWith(htmlBody, messageId);
    });

    it('should not apply transformations for text content', () => {
      const htmlBody = null;
      const textBody = 'Text content';
      const messageId = 'msg123';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain('Text content');
      expect(contentTransformationService.transformContent).not.toHaveBeenCalled();
    });

    it('should not apply transformations when message ID is empty', () => {
      const htmlBody = '<img src="cid:image123" alt="test">';
      const textBody = null;
      const messageId = '';
      themeService.isDarkTheme.and.returnValue(false);
      
      const result = service.getMessageContent(htmlBody, textBody, messageId);
      
      expect(result).toContain('<!DOCTYPE html>');
      expect(result).toContain(htmlBody);
      expect(contentTransformationService.transformContent).not.toHaveBeenCalled();
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

      expect(result).toContain('margin: 0');
      expect(result).toContain('padding: 4px');
      expect(result).toContain('box-sizing: border-box');
      // issue #154: long unbroken strings must not blow out the layout
      expect(result).toContain('overflow-wrap: break-word');
    });

    it('should not impose table styling on email layouts', () => {
      themeService.isDarkTheme.and.returnValue(false);
      const result = service.formatMessageContent('<p>Test</p>', 'text/html', 'msg123');

      // emails are built from tables; borders/padding/collapse of our own
      // would visibly break their design
      expect(result).not.toContain('border: 1px solid');
      expect(result).not.toContain('border-collapse');
      expect(result).not.toContain("font-family: 'Plus Jakarta Sans'");
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
      
      const mono = getComputedStyle(document.body).getPropertyValue('--pc-font-mono').trim()
        || `'Courier New', monospace`;

      expect(result).toContain(`font-family: ${mono}`);
      expect(result).toContain('white-space: pre-wrap');
      expect(result).toContain('word-wrap: break-word');
    });
  });
});
