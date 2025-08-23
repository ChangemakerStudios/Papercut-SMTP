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
import { MessageListHeaderComponent } from './message-list-header.component';
import { PaginationComponent } from '../pagination/pagination.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

describe('MessageListHeaderComponent', () => {
  let component: MessageListHeaderComponent;
  let fixture: ComponentFixture<MessageListHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MessageListHeaderComponent,
        PaginationComponent,
        MatProgressSpinnerModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageListHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default pagination values', () => {
    expect(component.pageSize).toBe(10);
    expect(component.pageStart).toBe(0);
    expect(component.currentPage).toBe(1);
    expect(component.totalPages).toBe(1);
    expect(component.totalCount).toBe(0);
    expect(component.pageSizeOptions).toEqual([10, 25, 50, 100]);
    expect(component.isLoading).toBe(false);
  });

  it('should emit pageSizeChange event', () => {
    const newPageSize = 25;
    const spy = spyOn(component.pageSizeChange, 'emit');
    
    component.pageSizeChange.emit(newPageSize);
    
    expect(spy).toHaveBeenCalledWith(newPageSize);
  });

  it('should emit pageChange event', () => {
    const newPage = 3;
    const spy = spyOn(component.pageChange, 'emit');
    
    component.pageChange.emit(newPage);
    
    expect(spy).toHaveBeenCalledWith(newPage);
  });

  it('should update inputs when values change', () => {
    component.pageSize = 50;
    component.currentPage = 2;
    component.totalCount = 100;
    component.isLoading = true;
    
    fixture.detectChanges();
    
    expect(component.pageSize).toBe(50);
    expect(component.currentPage).toBe(2);
    expect(component.totalCount).toBe(100);
    expect(component.isLoading).toBe(true);
  });
});
