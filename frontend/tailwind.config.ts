import type { Config } from 'tailwindcss';

export default {
  darkMode: ['class'],
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: 'var(--color-primary, #1E3A5F)',
          foreground: 'var(--color-on-primary, #FFFFFF)',
          hover: '#162C48',
          light: '#2B4F81'
        },
        secondary: {
          DEFAULT: 'var(--color-secondary, #2563EB)',
          foreground: 'var(--color-on-secondary, #FFFFFF)',
          hover: '#1D4ED8'
        },
        accent: {
          DEFAULT: 'var(--color-accent, #16A34A)',
          foreground: 'var(--color-on-accent, #000000)',
          hover: '#15803D'
        },
        destructive: {
          DEFAULT: 'var(--color-destructive, #DC2626)',
          foreground: 'var(--color-on-destructive, #FFFFFF)',
          hover: '#B91C1C'
        },
        background: 'var(--color-background, #F8FAFC)',
        foreground: 'var(--color-foreground, #0F172A)',
        surface: 'var(--color-card, #FFFFFF)',
        'surface-foreground': 'var(--color-card-foreground, #0F172A)',
        muted: {
          DEFAULT: 'var(--color-muted, #E9EEF5)',
          foreground: 'var(--color-muted-foreground, #475569)'
        },
        border: 'var(--color-border, #CBD5E1)',
        ring: 'var(--color-ring, #1E3A5F)'
      },
      fontFamily: {
        heading: ['Lexend', 'Inter', 'sans-serif'],
        body: ['Source Sans 3', 'system-ui', 'sans-serif'],
        sans: ['Source Sans 3', 'system-ui', 'sans-serif']
      },
      borderRadius: {
        lg: 'var(--radius, 0.5rem)',
        md: 'calc(var(--radius, 0.5rem) - 2px)',
        sm: 'calc(var(--radius, 0.5rem) - 4px)'
      },
      boxShadow: {
        subtle: '0 1px 2px rgba(0,0,0,0.05)',
        card: '0 4px 6px -1px rgba(0,0,0,0.06), 0 2px 4px -1px rgba(0,0,0,0.04)',
        modal: '0 10px 15px -3px rgba(0,0,0,0.1), 0 4px 6px -2px rgba(0,0,0,0.05)',
        glow: '0 0 15px rgba(30, 58, 95, 0.25)'
      },
      transitionDuration: {
        fast: '150ms',
        normal: '250ms',
        slow: '350ms'
      }
    }
  },
  plugins: []
} satisfies Config;
