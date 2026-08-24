<script setup lang="ts">
import type { ProductListItem } from "~/composables/useProducts";

withDefaults(
  defineProps<{
    products: ProductListItem[];
    loading?: boolean;
  }>(),
  {
    loading: false,
  },
);

defineEmits<{ delete: [product: ProductListItem] }>();

const resolveImage = useProductImage();
</script>

<template>
  <div class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
    <table class="w-full text-sm">
      <thead class="bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
        <tr>
          <th class="w-14 px-4 py-3 text-left"><span class="sr-only">Image</span></th>
          <th class="px-4 py-3 text-left font-medium">Name</th>
          <th class="px-4 py-3 text-right font-medium">Price</th>
          <th class="px-4 py-3 text-right font-medium">Stock</th>
          <th class="px-4 py-3 text-left font-medium">Status</th>
          <th class="px-4 py-3 text-right font-medium">Actions</th>
        </tr>
      </thead>

      <tbody v-if="loading" class="divide-y divide-gray-100">
        <tr v-for="n in 5" :key="n" class="animate-pulse">
          <td class="px-4 py-3">
            <div class="h-10 w-10 rounded bg-gray-100" />
          </td>
          <td class="px-4 py-3"><div class="h-4 w-40 rounded bg-gray-100" /></td>
          <td class="px-4 py-3"><div class="ml-auto h-4 w-16 rounded bg-gray-100" /></td>
          <td class="px-4 py-3"><div class="ml-auto h-4 w-10 rounded bg-gray-100" /></td>
          <td class="px-4 py-3"><div class="h-4 w-16 rounded bg-gray-100" /></td>
          <td class="px-4 py-3"><div class="ml-auto h-4 w-24 rounded bg-gray-100" /></td>
        </tr>
      </tbody>

      <tbody v-else-if="products.length === 0">
        <tr>
          <td colspan="6" class="px-4 py-8 text-center text-gray-500">
            No products yet.
          </td>
        </tr>
      </tbody>

      <tbody v-else class="divide-y divide-gray-100">
        <tr v-for="product in products" :key="product.id" class="hover:bg-gray-50">
          <td class="px-4 py-3">
            <img
              v-if="resolveImage(product.imageUrl)"
              :src="resolveImage(product.imageUrl)!"
              :alt="product.name"
              loading="lazy"
              class="h-10 w-10 rounded object-cover"
            />
            <div v-else class="h-10 w-10 rounded bg-gray-100" aria-hidden="true" />
          </td>
          <td class="px-4 py-3 font-medium text-gray-900">{{ product.name }}</td>
          <td class="px-4 py-3 text-right tabular-nums">
            {{ formatPrice(product.price) }}
          </td>
          <td class="px-4 py-3 text-right tabular-nums">{{ product.stock }}</td>
          <td class="px-4 py-3">
            <div class="flex flex-wrap items-center gap-1">
              <span
                v-if="product.isActive"
                class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700"
              >
                Active
              </span>
              <span
                v-else
                class="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600"
              >
                Inactive
              </span>
              <span
                v-if="product.stock <= 0"
                class="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700"
              >
                Out of stock
              </span>
            </div>
          </td>
          <td class="px-4 py-3">
            <div class="flex items-center justify-end gap-3">
              <NuxtLink
                :to="`/admin/products/${product.id}/edit`"
                class="font-medium text-primary hover:underline"
              >
                Edit
              </NuxtLink>
              <button
                type="button"
                class="font-medium text-red-600 hover:underline"
                @click="$emit('delete', product)"
              >
                Delete
              </button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
