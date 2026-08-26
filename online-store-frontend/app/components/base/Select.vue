<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    modelValue?: string;
    label?: string;
    error?: string;
    options: { value: string; label: string }[];
    placeholder?: string;
    required?: boolean;
  }>(),
  { modelValue: "", required: false, placeholder: "Select..." },
);

defineEmits<{ "update:modelValue": [value: string] }>();

// useId() is SSR-stable, so the <label for> association survives hydration.
const id = useId();
</script>

<template>
  <div class="space-y-1.5">
    <label v-if="label" :for="id" class="block text-sm font-medium text-[#1b3a6b]">
      {{ label }}
    </label>

    <select
      :id="id"
      :value="modelValue"
      :required="required"
      :aria-invalid="!!error"
      :aria-describedby="error ? `${id}-error` : undefined"
      :class="[
        'w-full rounded-lg px-4 py-3 text-sm text-gray-900',
        'border border-transparent transition focus:outline-none focus:ring-2',
        error
          ? 'bg-red-50 ring-1 ring-red-400 focus:ring-red-500'
          : 'bg-[#e8edf9] focus:border-[#1b3a6b] focus:ring-[#1b3a6b]/40',
      ]"
      @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
      <option value="" disabled>{{ placeholder }}</option>
      <option v-for="opt in options" :key="opt.value" :value="opt.value">
        {{ opt.label }}
      </option>
    </select>

    <p v-if="error" :id="`${id}-error`" class="text-xs text-red-600">
      {{ error }}
    </p>
  </div>
</template>
