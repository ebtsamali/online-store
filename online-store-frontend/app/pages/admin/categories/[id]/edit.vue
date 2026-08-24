<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";
import { nameFormSchema, fieldErrors } from "~/utils/validation";

interface CategoryDto {
  id: number;
  name: string;
}

const route = useRoute();
const apiBase = useApi();
const auth = useAuthStore();

const rawId = Number(route.params.id);
const isValidId = Number.isInteger(rawId) && rawId > 0;

const { data } = await useFetch<CategoryDto[]>(`${apiBase}/categories`, {
  key: "admin-categories-list",
  headers: { Authorization: `Bearer ${auth.token}` },
  server: false,
  default: (): CategoryDto[] => [],
});

const match = computed(() => data.value.find((c) => c.id === rawId) ?? null);
const isNotFound = computed(() => !isValidId || !match.value);

const form = reactive({ name: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

watch(
  match,
  (m) => {
    if (m) form.name = m.name;
  },
  { immediate: true },
);

async function onSubmit() {
  errors.value = {};

  const parsed = nameFormSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    await $fetch(`${apiBase}/categories/${rawId}`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${auth.token}` },
      body: parsed.data,
    });
    toast.success(`"${parsed.data.name}" updated.`);
    await navigateTo("/admin/categories");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not update category.");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-md space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Edit category</h1>

    <div v-if="isNotFound" class="rounded-xl border border-gray-200 p-8 text-center">
      <p class="text-gray-700">Category not found.</p>
      <div class="mt-4 flex justify-center">
        <NuxtLink to="/admin/categories">
          <BaseButton variant="secondary">Back to categories</BaseButton>
        </NuxtLink>
      </div>
    </div>

    <BaseForm v-else @submit="onSubmit">
      <BaseInput v-model="form.name" label="Name" :error="errors.name" />
      <BaseButton type="submit" :loading="loading">Save changes</BaseButton>
    </BaseForm>
  </div>
</template>
