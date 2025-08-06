import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavigationComponent } from './components/navigation/navigation.component';
import { BottomToolbarComponent } from './components/bottom-toolbar/bottom-toolbar.component';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavigationComponent, BottomToolbarComponent],
  template: `
    <div class="app-container">
      <app-navigation></app-navigation>
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
      <app-bottom-toolbar 
        [selectedMessageCount]="selectedMessageCount"
        [totalMessageCount]="totalMessageCount"
        (forward)="onForward()"
        (deleteSelected)="onDeleteSelected()"
        (deleteAll)="onDeleteAll()">
      </app-bottom-toolbar>
    </div>
  `,
  styles: []
})
export class AppComponent {
  title = 'Papercut';
  selectedMessageCount = 0;
  totalMessageCount = 0;

  constructor(private themeService: ThemeService) {
    // Initialize theme service
  }

  onForward(): void {
    // TODO: Implement forward functionality
    console.log('Forward clicked from toolbar');
  }

  onDeleteSelected(): void {
    // TODO: Implement delete selected functionality
    console.log('Delete selected clicked from toolbar');
  }

  onDeleteAll(): void {
    // TODO: Implement delete all functionality
    console.log('Delete all clicked from toolbar');
  }
} 