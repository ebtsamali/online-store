// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2025-07-15",
  devtools: { enabled: true },
  modules: ["@nuxtjs/tailwindcss", "@pinia/nuxt"],
  css: ["vue-sonner/style.css"],
  runtimeConfig: {
    public: {
      apiBase: "https://localhost:7225/api",
    },
  },
});
