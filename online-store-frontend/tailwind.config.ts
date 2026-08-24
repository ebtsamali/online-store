import type { Config } from "tailwindcss";

// @nuxtjs/tailwindcss merges its own `content` paths, so none is declared here.
//
// Follow-up: `app/pages/auth/login.vue`, `app/pages/auth/register.vue` and
// `app/components/base/Input.vue` still carry `#1b3a6b` / `#e8edf9` literals in
// their markup. Converting them to these tokens is deliberately out of scope of
// the home-page story and should be done as a separate pass.
export default {
  theme: {
    extend: {
      colors: {
        primary: {
          50: "#eef2fb",
          100: "#dbe3f5",
          200: "#b8c6ea",
          300: "#8fa4dc",
          400: "#4f6cbb",
          500: "#1C3684", // brand
          600: "#182f73",
          700: "#142862",
          800: "#101f4d",
          900: "#0c1839",
          DEFAULT: "#1C3684",
        },
      },
    },
  },
} satisfies Config;
