<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string;
    value: string | number;
    hint?: string;
    loading?: boolean;
  }>(),
  {
    hint: undefined,
    loading: false,
  },
);
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-4">
    <p class="text-xs uppercase tracking-wide text-gray-500">{{ label }}</p>
    <!-- Pulse bar instead of the value so the card never flashes 0 before data lands. -->
    <div v-if="loading" class="mt-2 h-8 w-16 animate-pulse rounded bg-gray-100" />
    <p v-else class="mt-1 text-2xl font-semibold text-gray-900">{{ value }}</p>
    <p v-if="hint" class="mt-1 text-xs text-gray-400">{{ hint }}</p>
  </div>
</template>
