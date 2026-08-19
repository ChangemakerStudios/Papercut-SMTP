/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      // All colors resolve to the Papercut token layer (src/styles/_tokens.scss)
      // so utilities, Material, and component styles share one design language.
      colors: {
        accent: 'var(--pc-accent)',
        'accent-text': 'var(--pc-accent-text)',
        'accent-soft': 'var(--pc-accent-soft)',
        chrome: 'var(--pc-chrome)',
        surface: 'var(--pc-surface)',
        'surface-2': 'var(--pc-surface-2)',
        page: 'var(--pc-bg)',
        ink: 'var(--pc-ink)',
        'ink-strong': 'var(--pc-ink-strong)',
        muted: 'var(--pc-muted)',
        faint: 'var(--pc-faint)',
        edge: 'var(--pc-border)',
        'edge-soft': 'var(--pc-border-soft)',
        hover: 'var(--pc-hover)',
        selected: 'var(--pc-selected)',
        danger: 'var(--pc-danger)',
        'danger-strong': 'var(--pc-danger-strong)',
        'danger-soft': 'var(--pc-danger-soft)',
        warn: 'var(--pc-warn)',
        'warn-soft': 'var(--pc-warn-soft)',
        ok: 'var(--pc-ok)',
      },
      fontFamily: {
        sans: ['Plus Jakarta Sans', '-apple-system', 'Segoe UI', 'sans-serif'],
        mono: ['JetBrains Mono', 'Cascadia Code', 'Consolas', 'monospace'],
      },
    },
  },
  plugins: [],
  darkMode: ['class', '[data-theme="dark"]'],
}
