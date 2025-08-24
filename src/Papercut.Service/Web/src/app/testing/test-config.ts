import { TestBed } from '@angular/core/testing';
import { SignalRService } from '../services/signalr.service';
import { PlatformNotificationService } from '../services/platform-notification.service';
import { createMockSignalRService } from './test-utils';

/**
 * Test configuration that provides mocks for external services
 */
export function configureTestEnvironment() {
  // Mock SignalR service to prevent real connections during testing
  const mockSignalR = createMockSignalRService();
  
  // Mock platform notification service
  const mockNotificationService = jasmine.createSpyObj('PlatformNotificationService', [
    'requestPermission',
    'showNotification',
    'isSupported'
  ]);
  mockNotificationService.isSupported.and.returnValue(true);
  mockNotificationService.requestPermission.and.returnValue(Promise.resolve('granted'));
  
  TestBed.configureTestingModule({
    providers: [
      { provide: SignalRService, useValue: mockSignalR },
      { provide: PlatformNotificationService, useValue: mockNotificationService }
    ]
  });
}

/**
 * Reset test environment after each test
 */
export function resetTestEnvironment() {
  // Clean up any remaining timers or async operations
  jasmine.clock().uninstall();
}
