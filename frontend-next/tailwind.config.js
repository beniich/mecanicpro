/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./styles/**/*.{js,ts,jsx,tsx,mdx,css}",
  ],
  theme: {
    extend: {
      colors: {
        primary: "#ff9159",
        "primary-container": "#ff7a2f",
        secondary: "#00eefc",
        tertiary: "#ac89ff",
        error: "#ff7351",
        surface: "#0e0e0e",
      },
      fontFamily: {
        headline: ["Space Grotesk", "sans-serif"],
        body: ["Inter", "sans-serif"],
        cond: ["Rajdhani", "sans-serif"],
      },
    },
  },
  plugins: [],
}
