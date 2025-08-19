import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { MessageService } from './message.service';
import { MessageRepository } from './message.repository';
import { GetMessagesResponse, DetailDto, RefDto } from '../models';
import { 
  mockMessages, 
  mockDetailDto, 
  mockGetMessagesResponse,
  mockErrorResponse 
} from '../testing/mock-data';

describe('MessageService', () => {
  let service: MessageService;
  let httpMock: HttpTestingController;
  let messageRepository: jasmine.SpyObj<MessageRepository>;

  beforeEach(() => {
    const repositorySpy = jasmine.createSpyObj('MessageRepository', [
      'getMessages',
      'getMessage',
      'downloadRawMessage'
    ]);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        MessageService,
        { provide: MessageRepository, useValue: repositorySpy }
      ]
    });

    service = TestBed.inject(MessageService);
    httpMock = TestBed.inject(HttpTestingController);
    messageRepository = TestBed.inject(MessageRepository) as jasmine.SpyObj<MessageRepository>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMessages', () => {
    it('should return messages with default pagination', (done) => {
      const expectedResponse = mockGetMessagesResponse;
      messageRepository.getMessages.and.returnValue(of(expectedResponse));

      service.getMessages().subscribe(response => {
        expect(response).toEqual(expectedResponse);
        expect(response.messages.length).toBe(3);
        expect(response.totalMessageCount).toBe(3);
        done();
      });
    });

    it('should return messages with custom pagination', (done) => {
      const limit = 5;
      const start = 10;
      // Create a response with exactly 5 messages to match the limit
      const additionalMessages = [
        { ...mockMessages[0], id: 'msg-004' },
        { ...mockMessages[0], id: 'msg-005' }
      ];
      const expectedResponse: GetMessagesResponse = { 
        totalMessageCount: 5,
        messages: [...mockMessages, ...additionalMessages] 
      };
      
      messageRepository.getMessages.and.returnValue(of(expectedResponse));

      service.getMessages(limit, start).subscribe(response => {
        expect(response.messages.length).toBe(5); // Should be 5, not 3
        expect(messageRepository.getMessages).toHaveBeenCalledWith({ limit: 5, start: 10 });
        done();
      });
    });

    it('should transform dates in response', (done) => {
      // Create test data with string dates that the repository would return
      const testDate = '2024-01-15T10:30:00Z';
      // Use type assertion to simulate repository response with string dates
      const repositoryResponse = {
        totalMessageCount: 3,
        messages: mockMessages.map(msg => ({
          ...msg,
          createdAt: testDate
        }))
      } as any; // Type assertion to bypass strict typing for test data

      messageRepository.getMessages.and.returnValue(of(repositoryResponse));

      service.getMessages().subscribe(response => {
        // The service should transform string dates to Date objects
        expect(response.messages[0].createdAt).toBeInstanceOf(Date);
        expect(response.messages[0].createdAt?.getTime()).toBe(new Date(testDate).getTime());
        done();
      });
    });

    it('should handle null dates gracefully', (done) => {
      // Use type assertion to simulate repository response with null dates
      const repositoryResponse = {
        totalMessageCount: 3,
        messages: mockMessages.map(msg => ({
          ...msg,
          createdAt: null
        }))
      } as any; // Type assertion to bypass strict typing for test data

      messageRepository.getMessages.and.returnValue(of(repositoryResponse));

      service.getMessages().subscribe(response => {
        expect(response.messages[0].createdAt).toBeNull();
        done();
      });
    });
  });

  describe('getMessageRef', () => {
    it('should find message by ID in recent messages', (done) => {
      const messageId = 'msg-001';
      const mockResponse: GetMessagesResponse = { ...mockGetMessagesResponse, messages: mockMessages };

      messageRepository.getMessages.and.returnValue(of(mockResponse));

      service.getMessageRef(messageId).subscribe(result => {
        expect(result).toBeTruthy();
        expect(result?.id).toBe(messageId);
        done();
      });
    });

    it('should return null for non-existent message ID', (done) => {
      const messageId = 'non-existent-id';
      const mockResponse: GetMessagesResponse = { ...mockGetMessagesResponse, messages: mockMessages };

      messageRepository.getMessages.and.returnValue(of(mockResponse));

      service.getMessageRef(messageId).subscribe(result => {
        expect(result).toBeNull();
        done();
      });
    });

    it('should search in recent 50 messages', (done) => {
      messageRepository.getMessages.and.returnValue(of(mockGetMessagesResponse));

      service.getMessageRef('msg-001').subscribe(() => {
        expect(messageRepository.getMessages).toHaveBeenCalledWith({ limit: 50, start: 0 });
        done();
      });
    });
  });

  describe('getMessage', () => {
    it('should return message details', (done) => {
      const messageId = 'msg-001';
      messageRepository.getMessage.and.returnValue(of(mockDetailDto));

      service.getMessage(messageId).subscribe(result => {
        expect(result).toEqual(mockDetailDto);
        expect(messageRepository.getMessage).toHaveBeenCalledWith(messageId);
        done();
      });
    });

    it('should transform dates in message details', (done) => {
      const messageId = 'msg-001';
      const testDate = '2024-01-15T10:30:00Z';
      // Use type assertion to simulate repository response with string date
      const repositoryResponse = {
        ...mockDetailDto,
        createdAt: testDate
      } as any; // Type assertion to bypass strict typing for test data

      messageRepository.getMessage.and.returnValue(of(repositoryResponse));

      service.getMessage(messageId).subscribe(result => {
        // The service should transform string dates to Date objects
        expect(result.createdAt).toBeInstanceOf(Date);
        expect(result.createdAt?.getTime()).toBe(new Date(testDate).getTime());
        done();
      });
    });

    it('should handle null dates in message details', (done) => {
      const messageId = 'msg-001';
      const detailWithNullDate: DetailDto = {
        ...mockDetailDto,
        createdAt: null
      };

      messageRepository.getMessage.and.returnValue(of(detailWithNullDate));

      service.getMessage(messageId).subscribe(result => {
        expect(result.createdAt).toBeNull();
        done();
      });
    });
  });

  describe('downloadRawMessage', () => {
    it('should call repository download method', () => {
      const messageId = 'msg-001';
      
      service.downloadRawMessage(messageId);
      
      expect(messageRepository.downloadRawMessage).toHaveBeenCalledWith(messageId);
    });
  });

  describe('error handling', () => {
    it('should handle repository errors gracefully', (done) => {
      const errorMessage = 'Repository error';
      messageRepository.getMessages.and.returnValue(throwError(() => new Error(errorMessage)));

      service.getMessages().subscribe({
        error: (error) => {
          expect(error.message).toBe(errorMessage);
          done();
        }
      });
    });
  });
});
