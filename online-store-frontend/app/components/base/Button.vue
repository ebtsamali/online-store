<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: "button" | "submit" | "reset";
    variant?: "primary" | "secondary" | "ghost";
    block?: boolean;
    loading?: boolean;
    disabled?: boolean;
  }>(),
  {
    type: "button",
    variant: "primary",
    block: false,
    loading: false,
    disabled: false,
  },
);

const variants: Record<string, string> = {
  primary: "bg-primary text-white hover:bg-primary-600 focus:ring-primary",
  secondary:
    "bg-primary-100 text-primary hover:bg-primary-200 focus:ring-primary",
  ghost: "bg-transparent text-primary hover:bg-primary-50 focus:ring-primary",
};
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :class="[
      'inline-flex items-center justify-center gap-2 rounded-lg px-5 py-3 text-sm font-semibold transition',
      'focus:outline-none focus:ring-2 focus:ring-offset-2',
      'disabled:cursor-not-allowed disabled:opacity-60',
      variants[variant],
      block ? 'w-full' : '',
    ]"
  >
    <svg
      v-if="loading"
      class="h-4 w-4 animate-spin"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle
        class="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        stroke-width="4"
      />
      <path
        class="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
      />
    </svg>
    <slot />
  </button>
</template>
