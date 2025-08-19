import { RefDto, DetailDto, EmailAddressDto, HeaderDto, EmailSectionDto, GetMessagesResponse } from '../models';

/**
 * Mock data for testing the email system
 */

export const mockEmailAddresses: EmailAddressDto[] = [
  {
    name: 'John Doe',
    address: 'john.doe@example.com'
  },
  {
    name: 'Jane Smith',
    address: 'jane.smith@example.com'
  },
  {
    name: 'Test User',
    address: 'test@example.com'
  }
];

export const mockHeaders: HeaderDto[] = [
  {
    name: 'From',
    value: 'john.doe@example.com'
  },
  {
    name: 'To',
    value: 'jane.smith@example.com'
  },
  {
    name: 'Subject',
    value: 'Test Email Subject'
  },
  {
    name: 'Date',
    value: 'Mon, 15 Jan 2024 10:30:00 +0000'
  },
  {
    name: 'Message-ID',
    value: '<test-message-123@example.com>'
  }
];

export const mockEmailSections: EmailSectionDto[] = [
  {
    id: 'section-1',
    mediaType: 'text/plain',
    fileName: null
  },
  {
    id: 'section-2',
    mediaType: 'text/html',
    fileName: null
  }
];

export const mockAttachments: EmailSectionDto[] = [
  {
    id: 'attachment-1',
    mediaType: 'application/pdf',
    fileName: 'document.pdf'
  },
  {
    id: 'attachment-2',
    mediaType: 'image/jpeg',
    fileName: 'image.jpg'
  }
];

export const mockRefDto: RefDto = {
  id: 'msg-001',
  name: 'Test Email',
  createdAt: new Date('2024-01-15T10:30:00Z'),
  subject: 'Test Email Subject',
  size: 1024,
  from: [mockEmailAddresses[0]],
  isRead: false,
  priority: 'normal',
  attachmentCount: 2
};

export const mockDetailDto: DetailDto = {
  id: 'msg-001',
  name: 'Test Email',
  createdAt: new Date('2024-01-15T10:30:00Z'),
  subject: 'Test Email Subject',
  size: 1024,
  from: [mockEmailAddresses[0]],
  isRead: false,
  priority: 'normal',
  attachmentCount: 2,
  to: [mockEmailAddresses[1]],
  cc: [],
  bcc: [],
  htmlBody: '<html><body><h1>Test Email</h1><p>This is an HTML email body for testing purposes.</p></body></html>',
  textBody: 'This is a plain text email body for testing purposes.',
  headers: mockHeaders,
  sections: mockEmailSections,
  attachments: mockAttachments
};

export const mockMessages: RefDto[] = [
  mockRefDto,
  {
    id: 'msg-002',
    name: 'Another Test Email',
    createdAt: new Date('2024-01-15T09:15:00Z'),
    subject: 'Another Test Subject',
    size: 512,
    from: [mockEmailAddresses[1]],
    isRead: true,
    priority: 'high',
    attachmentCount: 0
  },
  {
    id: 'msg-003',
    name: 'Third Test Email',
    createdAt: new Date('2024-01-15T08:00:00Z'),
    subject: 'Third Test Subject',
    size: 2048,
    from: [mockEmailAddresses[2]],
    isRead: false,
    priority: 'low',
    attachmentCount: 1
  }
];

export const mockGetMessagesResponse: GetMessagesResponse = {
  messages: mockMessages,
  totalMessageCount: 3
};

export const mockPaginatedResponse = (page: number, pageSize: number): GetMessagesResponse => {
  const start = page * pageSize;
  const end = start + pageSize;
  const messages = mockMessages.slice(start, end);
  
  return {
    messages,
    totalMessageCount: mockMessages.length
  };
};

export const mockErrorResponse = {
  error: 'Test error message',
  status: 500,
  message: 'Internal server error'
};

export const mockEmptyResponse: GetMessagesResponse = {
  messages: [],
  totalMessageCount: 0
};
