<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";

interface CategoryDto {
  id: number;
  name: string;
}

const apiBase = useApi();
const auth = useAuthStore();

const { data, pending, error, refresh } = await useFetch<CategoryDto[]>(
  `${apiBase}/categories`,
  {
    key: "admin-categories-list",
    headers: { Authorization: `Bearer ${auth.token}` },
    server: false,
    default: (): CategoryDto[] => [],
  },
);

async function onDelete(category: CategoryDto) {
  if (!window.confirm(`Delete "${category.name}"? This cannot be undone.`)) {
    return;
  }
  try {
    await $fetch(`${apiBase}/categories/${category.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success(`"${category.name}" deleted.`);
    await refresh();
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not delete category.");
  }
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Categories</h1>
      <NuxtLink to="/admin/categories/new">
        <BaseButton>Add category</BaseButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="rounded-xl border border-gray-200 p-8 text-center">
      <p class="text-gray-700">We couldn't load categories right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <div v-else-if="pending" class="space-y-3">
      <div v-for="n in 3" :key="n" class="h-12 animate-pulse rounded-lg bg-gray-100" />
    </div>

    <div
      v-else-if="data.length === 0"
      class="rounded-xl border border-gray-200 p-8 text-center text-gray-500"
    >
      No categories yet.
    </div>

    <div v-else class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
      <table class="w-full text-sm">
        <thead class="bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
          <tr>
            <th class="px-4 py-3 text-left font-medium">Name</th>
            <th class="px-4 py-3 text-right font-medium">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="category in data" :key="category.id" class="hover:bg-gray-50">
            <td class="px-4 py-3 font-medium text-gray-900">{{ category.name }}</td>
            <td class="px-4 py-3">
              <div class="flex items-center justify-end gap-3">
                <NuxtLink
                  :to="`/admin/categories/${category.id}/edit`"
                  class="font-medium text-primary hover:underline"
                >
                  Edit
                </NuxtLink>
                <button
                  type="button"
                  class="font-medium text-red-600 hover:underline"
                  @click="onDelete(category)"
                >
                  Delete
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
