import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageListItemComponent } from './message-list-item.component';
import { EmailService } from '../../services/email.service';
import { RefDto } from '../../models';

describe('MessageListItemComponent', () => {
  let component: MessageListItemComponent;
  let fixture: ComponentFixture<MessageListItemComponent>;
  let emailService: jasmine.SpyObj<EmailService>;

  beforeEach(async () => {
    const emailServiceSpy = jasmine.createSpyObj('EmailService', ['formatEmailAddressList']);
    
    await TestBed.configureTestingModule({
      imports: [MessageListItemComponent],
      providers: [
        { provide: EmailService, useValue: emailServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageListItemComponent);
    component = fixture.componentInstance;
    emailService = TestBed.inject(EmailService) as jasmine.SpyObj<EmailService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('getAttachmentTooltip', () => {
    it('should return "No attachments" when attachmentCount is 0', () => {
      component.message = {
        id: '1',
        attachmentCount: 0
      } as RefDto;

      const result = component.getAttachmentTooltip();
      expect(result).toBe('No attachments');
    });

    it('should return "Has 1 attachment" when attachmentCount is 1', () => {
      component.message = {
        id: '1',
        attachmentCount: 1
      } as RefDto;

      const result = component.getAttachmentTooltip();
      expect(result).toBe('Has 1 attachment');
    });

    it('should return "Has X attachments" when attachmentCount is greater than 1', () => {
      component.message = {
        id: '1',
        attachmentCount: 3
      } as RefDto;

      const result = component.getAttachmentTooltip();
      expect(result).toBe('Has 3 attachments');
    });

    it('should handle undefined attachmentCount gracefully', () => {
      component.message = {
        id: '1',
        attachmentCount: undefined
      } as RefDto;

      const result = component.getAttachmentTooltip();
      expect(result).toBe('No attachments');
    });

    it('should handle null attachmentCount gracefully', () => {
      component.message = {
        id: '1',
        attachmentCount: null
      } as RefDto;

      const result = component.getAttachmentTooltip();
      expect(result).toBe('No attachments');
    });
  });

  describe('getFromDisplay', () => {
    it('should call emailService.formatEmailAddressList with message from', () => {
      const mockFrom = [{ name: 'Test User', address: 'test@example.com' }];
      component.message = {
        id: '1',
        from: mockFrom
      } as RefDto;

      emailService.formatEmailAddressList.and.returnValue('Test User <test@example.com>');

      const result = component.getFromDisplay();

      expect(emailService.formatEmailAddressList).toHaveBeenCalledWith(mockFrom);
      expect(result).toBe('Test User <test@example.com>');
    });

    it('should handle undefined from gracefully', () => {
      component.message = {
        id: '1',
        from: undefined
      } as RefDto;

      emailService.formatEmailAddressList.and.returnValue('');

      const result = component.getFromDisplay();

      expect(emailService.formatEmailAddressList).toHaveBeenCalledWith([]);
      expect(result).toBe('');
    });
  });

  describe('onSelect', () => {
    it('should emit select event when not loading', () => {
      component.isLoading = false;
      spyOn(component.select, 'emit');

      component.onSelect();

      expect(component.select.emit).toHaveBeenCalled();
    });

    it('should not emit select event when loading', () => {
      component.isLoading = true;
      spyOn(component.select, 'emit');

      component.onSelect();

      expect(component.select.emit).not.toHaveBeenCalled();
    });
  });

  describe('hasStatusIndicators', () => {
    it('should return true when message has attachments', () => {
      component.message = {
        id: '1',
        attachmentCount: 2
      } as RefDto;

      const result = component.hasStatusIndicators();
      expect(result).toBe(true);
    });

    it('should return true when message has urgent priority', () => {
      component.message = {
        id: '1',
        priority: 'Urgent'
      } as RefDto;

      const result = component.hasStatusIndicators();
      expect(result).toBe(true);
    });

    it('should return true when message has non-urgent priority', () => {
      component.message = {
        id: '1',
        priority: 'Non-urgent'
      } as RefDto;

      const result = component.hasStatusIndicators();
      expect(result).toBe(true);
    });

    it('should return false when message has no status indicators', () => {
      component.message = {
        id: '1',
        attachmentCount: 0,
        priority: 'normal'
      } as RefDto;

      const result = component.hasStatusIndicators();
      expect(result).toBe(false);
    });
  });
});
