<script setup lang="ts">
const emit = defineEmits<{ submit: [] }>();
</script>

<template>
  <form class="space-y-5" novalidate @submit.prevent="emit('submit')">
    <slot />
  </form>
</template>
