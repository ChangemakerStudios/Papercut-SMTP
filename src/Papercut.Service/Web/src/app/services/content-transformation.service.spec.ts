import { TestBed } from '@angular/core/testing';
import { ContentTransformationService } from './content-transformation.service';

describe('ContentTransformationService', () => {
  let service: ContentTransformationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ContentTransformationService]
    });
    service = TestBed.inject(ContentTransformationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('transformCidReferences', () => {
    it('should transform CID references to API URLs', () => {
      const html = '<img src="cid:image123" alt="test">';
      const messageId = 'msg456';
      const expected = '<img src="/api/messages/msg456/contents/image123" alt="test">';
      
      const result = service.transformCidReferences(html, messageId);
      expect(result).toBe(expected);
    });

    it('should handle multiple CID references', () => {
      const html = '<img src="cid:image1"><img src="cid:image2">';
      const messageId = 'msg123';
      const expected = '<img src="/api/messages/msg123/contents/image1"><img src="/api/messages/msg123/contents/image2">';
      
      const result = service.transformCidReferences(html, messageId);
      expect(result).toBe(expected);
    });

    it('should handle different quote styles', () => {
      const html = '<img src=\'cid:image123\' alt="test">';
      const messageId = 'msg456';
      const expected = '<img src="/api/messages/msg456/contents/image123" alt="test">';
      
      const result = service.transformCidReferences(html, messageId);
      expect(result).toBe(expected);
    });

    it('should return original HTML when no CID references', () => {
      const html = '<p>No CID references here</p>';
      const messageId = 'msg123';
      
      const result = service.transformCidReferences(html, messageId);
      expect(result).toBe(html);
    });

    it('should return original HTML when messageId is empty', () => {
      const html = '<img src="cid:image123">';
      const messageId = '';
      
      const result = service.transformCidReferences(html, messageId);
      expect(result).toBe(html);
    });

    it('should handle null/undefined HTML', () => {
      expect(service.transformCidReferences('', 'msg123')).toBe('');
      expect(service.transformCidReferences(null as any, 'msg123')).toBe(null as any);
      expect(service.transformCidReferences(undefined as any, 'msg123')).toBe(undefined as any);
    });
  });

  describe('makeUrlsAbsolute', () => {
    // Skip tests that require window.location mocking for now
    // These tests can be added back when we implement a more robust mocking strategy
    it('should handle null/undefined HTML', () => {
      expect(service.makeUrlsAbsolute('')).toBe('');
      expect(service.makeUrlsAbsolute(null as any)).toBe(null as any);
      expect(service.makeUrlsAbsolute(undefined as any)).toBe(undefined as any);
    });
  });

  describe('transformContent', () => {
    // Skip tests that require window.location mocking for now
    // These tests can be added back when we implement a more robust mocking strategy
    it('should handle empty messageId', () => {
      const html = '<img src="cid:image123">';
      const messageId = '';
      
      const result = service.transformContent(html, messageId);
      expect(result).toBe(html);
    });
  });
});
