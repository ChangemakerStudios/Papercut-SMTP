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

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageListLoadingOverlayComponent } from './message-list-loading-overlay.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

describe('MessageListLoadingOverlayComponent', () => {
  let component: MessageListLoadingOverlayComponent;
  let fixture: ComponentFixture<MessageListLoadingOverlayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MessageListLoadingOverlayComponent,
        MatProgressSpinnerModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageListLoadingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });



  it('should have default values', () => {
    expect(component.isLoading).toBe(false);
    expect(component.loadingMessage).toBe('Loading message...');
  });

  it('should not show overlay when isLoading is false', () => {
    component.isLoading = false;
    fixture.detectChanges();
    
    const overlayElement = fixture.nativeElement.querySelector('.absolute');
    expect(overlayElement).toBeFalsy();
  });

  it('should show overlay when isLoading is true', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const overlayElement = fixture.nativeElement.querySelector('.absolute');
    expect(overlayElement).toBeTruthy();
  });

  it('should display the loading spinner when loading', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const spinnerElement = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinnerElement).toBeTruthy();
  });

  it('should display the default loading message when loading', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const messageElement = fixture.nativeElement.querySelector('span');
    expect(messageElement.textContent).toContain('Loading message...');
  });

  it('should display custom loading message when loading', () => {
    component.isLoading = true;
    component.loadingMessage = 'Custom loading message';
    fixture.detectChanges();
    
    const messageElement = fixture.nativeElement.querySelector('span');
    expect(messageElement.textContent).toContain('Custom loading message');
  });

  it('should have proper CSS classes for styling when loading', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const overlayElement = fixture.nativeElement.querySelector('.absolute');
    expect(overlayElement).toBeTruthy();
    expect(overlayElement.className).toContain('absolute');
    expect(overlayElement.className).toContain('inset-0');
    expect(overlayElement.className).toContain('bg-white/80');
    expect(overlayElement.className).toContain('dark:bg-gray-800/80');
    expect(overlayElement.className).toContain('backdrop-blur-sm');
    expect(overlayElement.className).toContain('z-10');
    expect(overlayElement.className).toContain('flex');
    expect(overlayElement.className).toContain('items-center');
    expect(overlayElement.className).toContain('justify-center');
  });
});
