import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MessageService } from './message.service';
import { MessageApiService } from './message-api.service';
import { ContentFormattingService } from './content-formatting.service';
import { ContentTransformationService } from './content-transformation.service';
import { GetMessagesResponse, RefDto, DetailDto, EmailAddressDto } from '../models';

describe('MessageService', () => {
  let service: MessageService;
  let messageApiService: jasmine.SpyObj<MessageApiService>;
  let contentFormattingService: jasmine.SpyObj<ContentFormattingService>;
  let contentTransformationService: jasmine.SpyObj<ContentTransformationService>;

  beforeEach(() => {
    const apiServiceSpy = jasmine.createSpyObj('MessageApiService', [
      'getMessages',
      'getMessageRef',
      'getMessageDetail',
      'downloadRawMessage',
      'downloadSectionByIndex',
      'downloadSectionByContentId',
      'downloadRawMessageWithProgress',
      'getSectionContent',
      'getSectionByIndex',
      'getRawContent',
      'deleteAllMessages'
    ]);
    
    const formattingServiceSpy = jasmine.createSpyObj('ContentFormattingService', [
      'getMessageContent',
      'formatMessageContent',
      'formatSectionContent'
    ]);
    
    const transformationServiceSpy = jasmine.createSpyObj('ContentTransformationService', [
      'transformContent'
    ]);

    TestBed.configureTestingModule({
      providers: [
        MessageService,
        { provide: MessageApiService, useValue: apiServiceSpy },
        { provide: ContentFormattingService, useValue: formattingServiceSpy },
        { provide: ContentTransformationService, useValue: transformationServiceSpy }
      ]
    });
    
    service = TestBed.inject(MessageService);
    messageApiService = TestBed.inject(MessageApiService) as jasmine.SpyObj<MessageApiService>;
    contentFormattingService = TestBed.inject(ContentFormattingService) as jasmine.SpyObj<ContentFormattingService>;
    contentTransformationService = TestBed.inject(ContentTransformationService) as jasmine.SpyObj<ContentTransformationService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMessages', () => {
    it('should return messages from API service', () => {
      const expectedResponse: GetMessagesResponse = {
        messages: [
          { 
            id: '1', 
            subject: 'Test 1', 
            from: [{ name: 'Test User', address: 'test1@example.com' }], 
            createdAt: new Date(), 
            attachmentCount: 0,
            size: 1024
          },
          { 
            id: '2', 
            subject: 'Test 2', 
            from: [{ name: 'Test User 2', address: 'test2@example.com' }], 
            createdAt: new Date(), 
            attachmentCount: 1,
            size: 2048
          }
        ],
        totalMessageCount: 2
      };
      
      messageApiService.getMessages.and.returnValue(of(expectedResponse));
      
      const result = service.getMessages(10, 0);
      
      result.subscribe(response => {
        expect(response).toEqual(expectedResponse);
      });
      
      expect(messageApiService.getMessages).toHaveBeenCalledWith(10, 0);
    });

    it('should use default parameters when none provided', () => {
      const expectedResponse: GetMessagesResponse = {
        messages: [],
        totalMessageCount: 0
      };
      
      messageApiService.getMessages.and.returnValue(of(expectedResponse));
      
      service.getMessages();
      
      expect(messageApiService.getMessages).toHaveBeenCalledWith(10, 0);
    });

    it('should pass custom parameters to API service', () => {
      const expectedResponse: GetMessagesResponse = {
        messages: [],
        totalMessageCount: 0
      };
      
      messageApiService.getMessages.and.returnValue(of(expectedResponse));
      
      service.getMessages(5, 10);
      
      expect(messageApiService.getMessages).toHaveBeenCalledWith(5, 10);
    });
  });

  describe('getMessageRef', () => {
    it('should return message ref from API service', () => {
      const mockRef: RefDto = {
        id: '123',
        subject: 'Test Subject',
        from: [{ name: 'Test User', address: 'test@example.com' }],
        createdAt: new Date(),
        attachmentCount: 0,
        size: 1024
      };
      
      messageApiService.getMessageRef.and.returnValue(of(mockRef));
      
      const result = service.getMessageRef('123');
      
      result.subscribe(ref => {
        expect(ref).toEqual(mockRef);
      });
      
      expect(messageApiService.getMessageRef).toHaveBeenCalledWith('123');
    });
  });

  describe('getMessage', () => {
    it('should return message detail from API service', () => {
      const mockDetail: DetailDto = {
        id: '123',
        subject: 'Test Subject',
        from: [{ name: 'Test User', address: 'test@example.com' }],
        to: [{ name: 'Recipient', address: 'recipient@example.com' }],
        cc: [],
        bcc: [],
        createdAt: new Date(),
        htmlBody: '<p>Test content</p>',
        textBody: 'Test content',
        headers: [],
        sections: [],
        attachments: [],
        size: 1024
      };
      
      messageApiService.getMessageDetail.and.returnValue(of(mockDetail));
      
      const result = service.getMessage('123');
      
      result.subscribe(detail => {
        expect(detail).toEqual(mockDetail);
      });
      
      expect(messageApiService.getMessageDetail).toHaveBeenCalledWith('123');
    });
  });

  describe('download operations', () => {
    it('should delegate downloadRawMessage to API service', () => {
      service.downloadRawMessage('123');
      expect(messageApiService.downloadRawMessage).toHaveBeenCalledWith('123');
    });

    it('should delegate downloadSectionByIndex to API service', () => {
      service.downloadSectionByIndex('123', 0);
      expect(messageApiService.downloadSectionByIndex).toHaveBeenCalledWith('123', 0);
    });

    it('should delegate downloadSectionByContentId to API service', () => {
      service.downloadSectionByContentId('123', 'content456');
      expect(messageApiService.downloadSectionByContentId).toHaveBeenCalledWith('123', 'content456');
    });

    it('should delegate downloadRawMessageWithProgress to API service', () => {
      service.downloadRawMessageWithProgress('123');
      expect(messageApiService.downloadRawMessageWithProgress).toHaveBeenCalledWith('123');
    });
  });

  describe('section content operations', () => {
    it('should delegate getSectionContent to API service', () => {
      const mockContent = 'Section content';
      messageApiService.getSectionContent.and.returnValue(of(mockContent));
      
      const result = service.getSectionContent('123', 'content456');
      
      result.subscribe(content => {
        expect(content).toEqual(mockContent);
      });
      
      expect(messageApiService.getSectionContent).toHaveBeenCalledWith('123', 'content456');
    });

    it('should delegate getSectionByIndex to API service', () => {
      const mockContent = 'Section content';
      messageApiService.getSectionByIndex.and.returnValue(of(mockContent));
      
      const result = service.getSectionByIndex('123', 0);
      
      result.subscribe(content => {
        expect(content).toEqual(mockContent);
      });
      
      expect(messageApiService.getSectionByIndex).toHaveBeenCalledWith('123', 0);
    });
  });

  describe('raw content operations', () => {
    it('should delegate getRawContent to API service', () => {
      const mockContent = 'Raw message content';
      messageApiService.getRawContent.and.returnValue(of(mockContent));
      
      const result = service.getRawContent('123');
      
      result.subscribe(content => {
        expect(content).toEqual(mockContent);
      });
      
      expect(messageApiService.getRawContent).toHaveBeenCalledWith('123');
    });
  });

  describe('delete operations', () => {
    it('should delegate deleteAllMessages to API service', () => {
      messageApiService.deleteAllMessages.and.returnValue(of(void 0));
      
      const result = service.deleteAllMessages();
      
      result.subscribe(() => {
        expect(messageApiService.deleteAllMessages).toHaveBeenCalled();
      });
    });
  });

  describe('content formatting', () => {
    it('should delegate getMessageContent to content formatting service', () => {
      const mockMessage: DetailDto = {
        id: '123',
        subject: 'Test',
        from: [{ name: 'Test User', address: 'test@example.com' }],
        to: [],
        cc: [],
        bcc: [],
        createdAt: new Date(),
        htmlBody: '<p>Test</p>',
        textBody: 'Test',
        headers: [],
        sections: [],
        attachments: [],
        size: 1024
      };
      
      const expectedContent = '<html><body>Formatted content</body></html>';
      contentFormattingService.getMessageContent.and.returnValue(expectedContent);
      
      const result = service.getMessageContent(mockMessage);
      
      expect(result).toEqual(expectedContent);
      expect(contentFormattingService.getMessageContent).toHaveBeenCalledWith(
        '<p>Test</p>',
        'Test',
        '123'
      );
    });
  });

  describe('error handling', () => {
    it('should propagate errors from API service', () => {
      const errorMessage = 'API Error';
      messageApiService.getMessages.and.returnValue(throwError(() => new Error(errorMessage)));
      
      service.getMessages().subscribe({
        next: () => fail('Should have errored'),
        error: (error) => {
          expect(error.message).toBe(errorMessage);
        }
      });
    });
  });
});
