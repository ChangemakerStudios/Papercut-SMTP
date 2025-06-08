import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '../../services/theme.service';
import { Observable, map } from 'rxjs';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatToolbarModule, 
    MatButtonModule, 
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <nav class="navbar fixed top-0 left-0 right-0 z-50 bg-gradient-to-r from-primary-600 to-primary-700 dark:from-gray-800 dark:to-gray-900 shadow-lg transition-colors duration-300">
      <div class="nav-container flex items-center justify-between w-full h-16 px-4 lg:px-6">
        <!-- Brand Section -->
        <div class="brand-section flex items-center flex-shrink-0" routerLink="/">
          <div class="logo-container flex items-center p-2 rounded-lg transition-all duration-200 hover:bg-white/10 dark:hover:bg-white/10 cursor-pointer">
            <div class="logo-background bg-gray-100 dark:bg-gray-700 rounded-md p-2 mr-3 transition-all duration-200">
              <img src="/assets/images/papercut-logo.png" 
                   alt="Papercut Logo" 
                   class="logo h-6 w-auto transition-transform duration-200 hover:scale-105">
            </div>
            <span class="brand-text text-xl font-medium text-white tracking-wide hidden sm:block">Papercut</span>
          </div>
        </div>
        
        <!-- Navigation Actions -->
        <div class="nav-actions flex items-center space-x-2 flex-shrink-0">
          <button mat-button 
                  routerLink="/" 
                  routerLinkActive="active" 
                  [routerLinkActiveOptions]="{exact: true}"
                  class="nav-button flex items-center space-x-2 px-4 py-2 rounded-lg transition-all duration-200 hover:bg-white/10 text-white">
            <mat-icon class="text-white">inbox</mat-icon>
            <span class="font-medium hidden sm:inline">Messages</span>
          </button>
          
          <!-- Theme Toggle Button -->
          <button mat-icon-button 
                  (click)="toggleTheme()"
                  matTooltip="{{ (isDarkTheme$ | async) ? 'Switch to Light Theme' : 'Switch to Dark Theme' }}"
                  class="theme-toggle-btn text-white hover:bg-white/10 transition-all duration-200">
            <mat-icon>{{ (isDarkTheme$ | async) ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
          
          <!-- Mobile Menu Button (for future expansion) -->
          <button mat-icon-button class="mobile-menu-btn lg:hidden text-white hover:bg-white/10">
            <mat-icon>menu</mat-icon>
          </button>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      height: 64px;
    }

    .nav-container {
      max-width: 100%;
    }

    .logo-background {
      @apply transition-all duration-200;
    }

    .logo-background:hover {
      @apply bg-gray-200 dark:bg-gray-600 shadow-sm;
    }

    .brand-section:hover .logo {
      transform: scale(1.05);
    }

    .nav-button.active {
      @apply bg-white/20 backdrop-blur-sm;
    }

    .nav-button {
      min-width: auto;
    }

    .theme-toggle-btn {
      @apply transition-all duration-200;
    }

    .theme-toggle-btn:hover {
      @apply bg-white/10 scale-110;
    }

    @media (max-width: 640px) {
      .brand-text {
        display: none;
      }
      
      .nav-container {
        padding-left: 1rem;
        padding-right: 1rem;
      }
      
      .logo-background {
        margin-right: 0.5rem;
        padding: 0.375rem;
      }
      
      .logo {
        height: 1.25rem;
      }
    }
  `]
})
export class NavigationComponent {
  isDarkTheme$: Observable<boolean>;

  constructor(private themeService: ThemeService) {
    this.isDarkTheme$ = this.themeService.theme$.pipe(
      map(theme => theme === 'dark')
    );
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
} 