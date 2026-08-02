<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    modelValue?: string;
    label?: string;
    type?: string;
    placeholder?: string;
    error?: string;
    autocomplete?: string;
    required?: boolean;
  }>(),
  { modelValue: "", type: "text", required: false },
);

defineEmits<{ "update:modelValue": [value: string] }>();

// useId() is SSR-stable, so the <label for> association survives hydration.
const id = useId();
const revealed = ref(false);

// The `type` prop is never mutated — the eye toggle only changes what we render.
const effectiveType = computed(() =>
  props.type === "password" && revealed.value ? "text" : props.type,
);
</script>

<template>
  <div class="space-y-1.5">
    <div v-if="label" class="flex items-center justify-between">
      <label :for="id" class="block text-sm font-medium text-[#1b3a6b]">
        {{ label }}
      </label>
      <slot name="label-aside" />
    </div>

    <div class="relative">
      <input
        :id="id"
        :type="effectiveType"
        :value="modelValue"
        :placeholder="placeholder"
        :autocomplete="autocomplete"
        :required="required"
        :aria-invalid="!!error"
        :aria-describedby="error ? `${id}-error` : undefined"
        :class="[
          'w-full rounded-lg px-4 py-3 text-sm text-gray-900 placeholder:text-[#8a94a6]',
          'border border-transparent transition focus:outline-none focus:ring-2',
          type === 'password' ? 'pr-11' : '',
          error
            ? 'bg-red-50 ring-1 ring-red-400 focus:ring-red-500'
            : 'bg-[#e8edf9] focus:border-[#1b3a6b] focus:ring-[#1b3a6b]/40',
        ]"
        @input="
          $emit('update:modelValue', ($event.target as HTMLInputElement).value)
        "
      />

      <button
        v-if="type === 'password'"
        type="button"
        class="absolute inset-y-0 right-0 flex items-center px-3 text-[#8a94a6] hover:text-[#1b3a6b]"
        :aria-label="revealed ? 'Hide password' : 'Show password'"
        @click="revealed = !revealed"
      >
        <svg
          v-if="!revealed"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.7"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M2.036 12.322a1 1 0 010-.644C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178a1 1 0 010 .644C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.964-7.178z"
          />
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
          />
        </svg>
        <svg
          v-else
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.7"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.774 3.162 10.066 7.5a10.52 10.52 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243"
          />
        </svg>
      </button>
    </div>

    <p v-if="error" :id="`${id}-error`" class="text-xs text-red-600">
      {{ error }}
    </p>
  </div>
</template>
