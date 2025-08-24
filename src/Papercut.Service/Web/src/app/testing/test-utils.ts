import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Component, Type, Input, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { MessageApiService } from '../services/message-api.service';
import { 
  mockMessages, 
  mockDetailDto, 
  mockGetMessagesResponse,
  mockErrorResponse 
} from './mock-data';

/**
 * Test utilities for Angular testing
 */

/**
 * Creates a test module with common testing imports
 */
export function createTestModule() {
  return TestBed.configureTestingModule({
    imports: [
      HttpClientTestingModule,
      RouterTestingModule,
      NoopAnimationsModule
    ]
  });
}

/**
 * Creates a test module for standalone components
 */
export function createStandaloneTestModule(component: Type<any>) {
  return TestBed.configureTestingModule({
    imports: [
      component,
      HttpClientTestingModule,
      RouterTestingModule,
      NoopAnimationsModule
    ]
  });
}

/**
 * Creates a mock MessageApiService to prevent real HTTP calls during testing
 */
export function createMockMessageApiService(): jasmine.SpyObj<MessageApiService> {
  const mockApiService = jasmine.createSpyObj('MessageApiService', [
    'getMessages',
    'getMessageRef',
    'getMessageDetail',
    'downloadRawMessage',
    'downloadSectionByContentId',
    'downloadSectionByIndex',
    'downloadRawMessageWithProgress',
    'getRawContent',
    'getSectionContent',
    'getSectionByIndex',
    'deleteAllMessages'
  ]);

  // Set up default return values
  mockApiService.getMessages.and.returnValue(of(mockGetMessagesResponse));
  mockApiService.getMessageRef.and.returnValue(of(mockMessages[0]));
  mockApiService.getMessageDetail.and.returnValue(of(mockDetailDto));
  mockApiService.getRawContent.and.returnValue(of('Mock raw content'));
  mockApiService.getSectionContent.and.returnValue(of('Mock section content'));
  mockApiService.getSectionByIndex.and.returnValue(of('Mock section content'));
  mockApiService.deleteAllMessages.and.returnValue(of(void 0));

  return mockApiService;
}

/**
 * Creates a mock ActivatedRoute with optional parameters
 */
export function createMockActivatedRoute(params: any = {}, queryParams: any = {}) {
  const paramsSubject = new Subject<any>();
  const queryParamsSubject = new Subject<any>();
  
  return {
    params: paramsSubject.asObservable(),
    queryParams: queryParamsSubject.asObservable(),
    paramsSubject: paramsSubject, // Expose subject for test control
    queryParamsSubject: queryParamsSubject, // Expose subject for test control
    snapshot: {
      params,
      queryParams,
      url: [],
      fragment: null
    }
  };
}

/**
 * Creates a mock router with navigation methods
 */
export function createMockRouter() {
  return {
    navigate: jasmine.createSpy('navigate'),
    navigateByUrl: jasmine.createSpy('navigateByUrl'),
    url: '/test',
    events: of(null)
  };
}

/**
 * Creates a mock SignalR service to prevent real connections during testing
 */
export function createMockSignalRService() {
  return {
    start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
    stop: jasmine.createSpy('stop').and.returnValue(Promise.resolve()),
    on: jasmine.createSpy('on'),
    off: jasmine.createSpy('off'),
    invoke: jasmine.createSpy('invoke'),
    connectionState: 'Disconnected',
    isConnected: false
  };
}

/**
 * Waits for async operations to complete
 */
export async function waitForAsync() {
  await new Promise(resolve => setTimeout(resolve, 0));
}

/**
 * Triggers change detection manually
 */
export function triggerChangeDetection(fixture: ComponentFixture<any>) {
  fixture.detectChanges();
  fixture.whenStable();
}

/**
 * Creates a mock service with spy methods
 */
export function createMockService<T>(serviceClass: new (...args: any[]) => T): jasmine.SpyObj<T> {
  const service = jasmine.createSpyObj(serviceClass.name, []);
  
  // Add common methods that might be used
  if ('getMessages' in service) {
    (service as any).getMessages.and.returnValue(of({ messages: [], totalCount: 0, pageStart: 0, pageSize: 10 }));
  }
  
  if ('getMessage' in service) {
    (service as any).getMessage.and.returnValue(of(null));
  }
  
  return service;
}

/**
 * Simulates user input on form elements
 */
export function simulateInput(element: HTMLElement, value: string) {
  const input = element as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('change'));
}

/**
 * Simulates click on an element
 */
export function simulateClick(element: HTMLElement) {
  element.click();
}

/**
 * Simulates keyboard events
 */
export function simulateKeyPress(element: HTMLElement, key: string, keyCode: number) {
  element.dispatchEvent(new KeyboardEvent('keydown', { key, keyCode }));
  element.dispatchEvent(new KeyboardEvent('keyup', { key, keyCode }));
}

/**
 * Creates a mock HTTP response
 */
export function createMockHttpResponse<T>(data: T, status: number = 200) {
  return {
    body: data,
    status,
    statusText: status === 200 ? 'OK' : 'Error',
    ok: status >= 200 && status < 300
  };
}

/**
 * Creates a mock HTTP error response
 */
export function createMockHttpError(status: number = 500, message: string = 'Internal Server Error') {
  return {
    error: { message },
    status,
    statusText: 'Error',
    ok: false
  };
}

/**
 * Waits for a specific condition to be true
 */
export async function waitFor(condition: () => boolean, timeout: number = 1000): Promise<void> {
  const start = Date.now();
  
  while (!condition() && (Date.now() - start) < timeout) {
    await new Promise(resolve => setTimeout(resolve, 10));
  }
  
  if (!condition()) {
    throw new Error(`Condition not met within ${timeout}ms`);
  }
}

/**
 * Creates a test component for testing purposes
 */
@Component({
  template: '<ng-content></ng-content>',
  standalone: true
})
export class TestComponent {}

/**
 * Helper to create a component fixture with proper setup
 */
export function createComponentFixture<T>(component: Type<T>): ComponentFixture<T> {
  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  return fixture;
}

/**
 * Mock components are now created inline in test files to avoid selector conflicts
 */
