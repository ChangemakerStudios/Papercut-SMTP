import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type Theme = 'light' | 'dark';

export interface AccentColor {
  name: string;
  value: string;
}

/**
 * Theme Accent palette — mirrors the desktop app's "Theme Accent" option.
 * Any value works in light and dark because all derived colors are computed
 * from --pc-accent with color-mix() in the token layer.
 */
export const ACCENT_COLORS: AccentColor[] = [
  { name: 'Steel Blue', value: '#4682b4' },
  { name: 'Papercut Blue', value: '#3478b2' },
  { name: 'Teal', value: '#0d8a8a' },
  { name: 'Spring Green', value: '#2eaf6c' },
  { name: 'Yellow Green', value: '#8aab2e' },
  { name: 'Sienna', value: '#a0522d' },
  { name: 'Tomato', value: '#e0553d' },
  { name: 'Crimson', value: '#c22347' },
  { name: 'Violet', value: '#b04ab0' },
  { name: 'Slate Blue', value: '#6a5acd' },
  { name: 'Slate Gray', value: '#708090' },
  { name: 'Thistle', value: '#a98ba9' },
];

const THEME_STORAGE_KEY = 'papercut-theme';
const ACCENT_STORAGE_KEY = 'papercut-accent';
const DEFAULT_ACCENT = ACCENT_COLORS[0];

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private currentTheme = new BehaviorSubject<Theme>('light');
  public theme$ = this.currentTheme.asObservable();

  private currentAccent = new BehaviorSubject<AccentColor>(DEFAULT_ACCENT);
  public accent$ = this.currentAccent.asObservable();

  readonly accentColors = ACCENT_COLORS;

  constructor() {
    const savedTheme = localStorage.getItem(THEME_STORAGE_KEY) as Theme;
    const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    const initialTheme = savedTheme || (systemPrefersDark ? 'dark' : 'light');
    this.setTheme(initialTheme);

    const savedAccent = localStorage.getItem(ACCENT_STORAGE_KEY);
    const initialAccent = ACCENT_COLORS.find(a => a.name === savedAccent) || DEFAULT_ACCENT;
    this.setAccent(initialAccent);
  }

  setTheme(theme: Theme): void {
    this.currentTheme.next(theme);
    localStorage.setItem(THEME_STORAGE_KEY, theme);

    // Update document class for Tailwind dark mode
    if (theme === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }

    // Token layer + Material theme switch on this attribute
    document.body.setAttribute('data-theme', theme);
  }

  toggleTheme(): void {
    const newTheme = this.currentTheme.value === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }

  getCurrentTheme(): Theme {
    return this.currentTheme.value;
  }

  isDarkTheme(): boolean {
    return this.currentTheme.value === 'dark';
  }

  setAccent(accent: AccentColor): void {
    this.currentAccent.next(accent);
    localStorage.setItem(ACCENT_STORAGE_KEY, accent.name);

    // The token layer derives every accent-tinted color from this variable
    document.documentElement.style.setProperty('--pc-accent', accent.value);
  }

  getCurrentAccent(): AccentColor {
    return this.currentAccent.value;
  }
}
