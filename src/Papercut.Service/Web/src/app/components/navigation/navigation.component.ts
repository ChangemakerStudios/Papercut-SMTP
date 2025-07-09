import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '../../services/theme.service';
import { Observable, map, switchMap, of } from 'rxjs';

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
    <nav class="navbar">
      <div class="nav-container">
        <!-- Brand Section -->
        <div class="logo-background" routerLink="/">
          <div class="logo-container">
            <img src="/assets/images/papercut-logo.png"
                 alt="Papercut Logo">
            <span class="brand-text">Papercut</span>
          </div>
        </div>
        
        <!-- Navigation Actions -->
        <div class="nav-actions">
          <button mat-button 
                  routerLink="/" 
                  routerLinkActive="active" 
                  [routerLinkActiveOptions]="{exact: true}"
                  class="nav-button">
            <mat-icon>inbox</mat-icon>
            <span>Messages</span>
          </button>
          
          <!-- Theme Toggle Button -->
          <button mat-icon-button 
                  (click)="toggleTheme()"
                  matTooltip="{{ (isDarkTheme$ | async) ? 'Switch to Light Theme' : 'Switch to Dark Theme' }}"
                  class="theme-toggle-btn">
            <mat-icon>{{ (isDarkTheme$ | async) ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
          
          <!-- Mobile Menu Button (for future expansion) -->
          <button mat-icon-button class="mobile-menu-btn">
            <mat-icon>menu</mat-icon>
          </button>
        </div>
      </div>
    </nav>
  `,
  styles: []
})
export class NavigationComponent implements OnDestroy {
  isDarkTheme$: Observable<boolean>;
  loadingTimeout: any;
  isLoadingMessage = false;

  constructor(private themeService: ThemeService) {
    this.isDarkTheme$ = this.themeService.theme$.pipe(
      map(theme => theme === 'dark')
    );
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  ngOnDestroy() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
    }
  }
} 