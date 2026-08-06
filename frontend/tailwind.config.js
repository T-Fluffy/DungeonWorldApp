/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        charcoal: '#1A1A1D',
        ember: '#FF6B35',
        crimson: '#C1121F',
        dark: {
          900: '#000000',
          800: '#1A1A1D',
          700: '#2D2D35',
        }
      },
      fontFamily: {
        gothic: ['"Pirata One"', 'cursive'],
        sans: ['Inter', 'sans-serif'],
      },
      animation: {
        'flicker': 'flicker 4s infinite alternate',
        'fog-drift': 'fogDrift 60s linear infinite',
        'pulse-slow': 'pulse 4s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'float': 'float 6s ease-in-out infinite',
      },
      keyframes: {
        flicker: {
          '0%, 100%': { opacity: 1, filter: 'brightness(1)' },
          '25%': { opacity: 0.85, filter: 'brightness(0.9)' },
          '50%': { opacity: 0.7, filter: 'brightness(0.8)' },
          '75%': { opacity: 0.9, filter: 'brightness(0.95)' },
        },
        fogDrift: {
          '0%': { transform: 'translateX(-10%)' },
          '100%': { transform: 'translateX(10%)' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' },
        }
      },
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
      }
    },
  },
  plugins: [],
}