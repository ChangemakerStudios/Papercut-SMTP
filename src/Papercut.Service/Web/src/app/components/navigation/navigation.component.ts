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
    <nav class="papercut-navbar">
      <div class="nav-container">
        <!-- Brand Section -->
        <div class="logo-section">
          <div class="logo-container" routerLink="/">
            <img [src]="(isDarkTheme$ | async) ? '/assets/images/papercut-logo-dark.png' : '/assets/images/papercut-logo-light.png'" 
                 alt="Papercut Logo" 
                 class="papercut-logo">
            <span class="brand-text">PAPERCUT SMTP</span>
          </div>
        </div>
        
        <!-- Navigation Actions (Desktop Layout Style) -->
        <div class="nav-actions">
          <button mat-stroked-button class="papercut-nav-btn" (click)="showLog()">
            <mat-icon>list_alt</mat-icon>
            <span>Log</span>
          </button>
          
          <button mat-stroked-button class="papercut-nav-btn" (click)="showRules()">
            <mat-icon>rule</mat-icon>
            <span>Rules</span>
          </button>
          
          <button mat-stroked-button class="papercut-nav-btn" (click)="showOptions()">
            <mat-icon>settings</mat-icon>
            <span>Options</span>
          </button>
          
          <!-- Theme Toggle integrated into buttons -->
          <button mat-stroked-button 
                  (click)="toggleTheme()"
                  matTooltip="{{ (isDarkTheme$ | async) ? 'Switch to Light Theme' : 'Switch to Dark Theme' }}"
                  class="papercut-nav-btn">
            <mat-icon>{{ (isDarkTheme$ | async) ? 'light_mode' : 'dark_mode' }}</mat-icon>
            <span>Theme</span>
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

  showLog(): void {
    // TODO: Implement log functionality
    console.log('Show Log clicked');
  }

  showRules(): void {
    // TODO: Implement rules functionality
    console.log('Show Rules clicked');
  }

  showOptions(): void {
    // TODO: Implement options functionality
    console.log('Show Options clicked');
  }



  ngOnDestroy() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
    }
  }
} 