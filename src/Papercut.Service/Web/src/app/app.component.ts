import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavigationComponent } from './components/navigation/navigation.component';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavigationComponent],
  template: `
    <div class="app-container h-screen overflow-hidden flex flex-col bg-gray-50 dark:bg-gray-900 transition-colors duration-300">
      <app-navigation></app-navigation>
      <main class="main-content flex-1 mt-16 min-h-0 bg-gray-50 dark:bg-gray-900">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      @apply transition-colors duration-300;
    }

    .main-content {
      @apply transition-colors duration-300;
    }

    // Ensure dark theme compatibility
    :host-context(body[data-theme="dark"]) .app-container {
      background-color: #121212;
    }

    :host-context(body[data-theme="dark"]) .main-content {
      background-color: #121212;
    }
  `]
})
export class AppComponent {
  title = 'Papercut';

  constructor(private themeService: ThemeService) {
    // Initialize theme service
  }
} 