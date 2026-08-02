<script setup lang="ts">
import { Toaster } from "vue-sonner";
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <!-- Mounted outside <NuxtLayout> so pages with `layout: false` still get toasts. -->
  <Toaster position="top-center" rich-colors />
</template>
