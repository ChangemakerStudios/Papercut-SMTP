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
import { MessageListEmptyStateComponent } from './message-list-empty-state.component';
import { MatIconModule } from '@angular/material/icon';

describe('MessageListEmptyStateComponent', () => {
  let component: MessageListEmptyStateComponent;
  let fixture: ComponentFixture<MessageListEmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MessageListEmptyStateComponent,
        MatIconModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageListEmptyStateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the inbox icon', () => {
    const iconElement = fixture.nativeElement.querySelector('mat-icon');
    expect(iconElement).toBeTruthy();
    expect(iconElement.textContent).toContain('inbox');
  });

  it('should display the "No Messages" heading', () => {
    const headingElement = fixture.nativeElement.querySelector('h3');
    expect(headingElement).toBeTruthy();
    expect(headingElement.textContent).toContain('No Messages');
  });

  it('should display the main description text', () => {
    const descriptionElement = fixture.nativeElement.querySelector('p');
    expect(descriptionElement).toBeTruthy();
    expect(descriptionElement.textContent).toContain('No emails have been received yet');
  });

  it('should display the secondary description text', () => {
    const secondaryDescriptionElement = fixture.nativeElement.querySelectorAll('p')[1];
    expect(secondaryDescriptionElement).toBeTruthy();
    expect(secondaryDescriptionElement.textContent).toContain('Messages will appear here when they arrive');
  });

  it('should have proper CSS classes for styling', () => {
    const containerElement = fixture.nativeElement.querySelector('div');
    expect(containerElement.className).toContain('flex');
    expect(containerElement.className).toContain('flex-col');
    expect(containerElement.className).toContain('items-center');
    expect(containerElement.className).toContain('justify-center');
  });
});
