<script setup lang="ts">
import type { ProductListItem } from "~/composables/useProducts";

const props = defineProps<{
  product: ProductListItem;
}>();

const resolveImage = useProductImage();
const imageSrc = computed(() => resolveImage(props.product.imageUrl));
const initial = computed(() => props.product.name.trim().charAt(0).toUpperCase());
</script>

<template>
  <NuxtLink
    :to="`/products/${product.id}`"
    class="group block overflow-hidden rounded-xl border border-gray-200 bg-white transition hover:shadow-md"
  >
    <img
      v-if="imageSrc"
      :src="imageSrc"
      :alt="product.name"
      loading="lazy"
      class="aspect-square w-full rounded-t-xl object-cover"
    />
    <div
      v-else
      class="flex aspect-square w-full items-center justify-center rounded-t-xl bg-primary-50 text-4xl font-bold text-primary-300"
      aria-hidden="true"
    >
      {{ initial }}
    </div>

    <div class="space-y-2 p-4">
      <h3 class="line-clamp-2 text-sm font-medium text-gray-900">
        {{ product.name }}
      </h3>
      <p class="font-semibold text-primary">{{ formatPrice(product.price) }}</p>
      <span
        v-if="product.stock <= 0"
        class="inline-block rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-500"
      >
        Out of stock
      </span>
      <p v-else class="text-xs text-gray-500">{{ product.stock }} in stock</p>
    </div>
  </NuxtLink>
</template>
