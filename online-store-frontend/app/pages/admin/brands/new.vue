<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";
import { nameFormSchema, fieldErrors } from "~/utils/validation";

const apiBase = useApi();
const auth = useAuthStore();

const form = reactive({ name: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

async function onSubmit() {
  errors.value = {};

  const parsed = nameFormSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    await $fetch(`${apiBase}/brands`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body: parsed.data,
    });
    toast.success(`"${parsed.data.name}" created.`);
    await navigateTo("/admin/brands");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not create brand.");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-md space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Add brand</h1>

    <BaseForm @submit="onSubmit">
      <BaseInput v-model="form.name" label="Name" :error="errors.name" />
      <BaseButton type="submit" :loading="loading">Create brand</BaseButton>
    </BaseForm>
  </div>
</template>
