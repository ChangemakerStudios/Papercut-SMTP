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
import { MessageListNoSelectionComponent } from './message-list-no-selection.component';
import { MatIconModule } from '@angular/material/icon';

describe('MessageListNoSelectionComponent', () => {
  let component: MessageListNoSelectionComponent;
  let fixture: ComponentFixture<MessageListNoSelectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MessageListNoSelectionComponent,
        MatIconModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageListNoSelectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the email icon', () => {
    const iconElement = fixture.nativeElement.querySelector('mat-icon');
    expect(iconElement).toBeTruthy();
    expect(iconElement.textContent).toContain('email');
  });

  it('should display the "No message selected" heading', () => {
    const headingElement = fixture.nativeElement.querySelector('h3');
    expect(headingElement).toBeTruthy();
    expect(headingElement.textContent).toContain('No message selected');
  });

  it('should display the description text', () => {
    const descriptionElement = fixture.nativeElement.querySelector('p');
    expect(descriptionElement).toBeTruthy();
    expect(descriptionElement.textContent).toContain('Select a message from the list to view its contents');
  });

  it('should have proper CSS classes for styling', () => {
    const containerElement = fixture.nativeElement.querySelector('div');
    expect(containerElement.className).toContain('flex-1');
    expect(containerElement.className).toContain('flex');
    expect(containerElement.className).toContain('flex-col');
    expect(containerElement.className).toContain('items-center');
    expect(containerElement.className).toContain('justify-center');
    expect(containerElement.className).toContain('p-8');
  });
});
