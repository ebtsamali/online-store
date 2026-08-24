<script setup lang="ts">
import { toast } from "vue-sonner";

const route = useRoute();
const auth = useAuthStore();
const apiBase = useApi();
const resolveImage = useProductImage();

const rawId = Number(route.params.id);
// A non-numeric or non-positive id can never match the backend's
// `[HttpGet("{id:int}")]` route — treat it as "not found" without a request.
const isValidId = Number.isInteger(rawId) && rawId > 0;

const { data: product, pending, error, refresh } = isValidId
  ? useProduct(rawId)
  : { data: ref(null), pending: ref(false), error: ref({ statusCode: 404 }), refresh: () => {} };

const quantity = ref(1);
const addingToCart = ref(false);

const imageSrc = computed(() => resolveImage(product.value?.imageUrl));
const initial = computed(() => product.value?.name.trim().charAt(0).toUpperCase() ?? "");

const isNotFound = computed(() => (error.value as any)?.statusCode === 404);

watch(product, (p) => {
  if (p) quantity.value = Math.min(quantity.value, Math.max(p.stock, 1));
});

function incrementQuantity() {
  if (product.value) quantity.value = Math.min(quantity.value + 1, product.value.stock);
}
function decrementQuantity() {
  quantity.value = Math.max(quantity.value - 1, 1);
}

async function addToCart() {
  if (!product.value) return;
  if (!auth.isAuthenticated) {
    await navigateTo("/auth/login");
    return;
  }

  addingToCart.value = true;
  try {
    await $fetch(`${apiBase}/cart`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body: { productId: product.value.id, quantity: quantity.value },
    });
    toast.success("Added to cart");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not add this item to your cart");
  } finally {
    addingToCart.value = false;
  }
}
</script>

<template>
  <div class="space-y-8">
    <NuxtLink to="/products" class="text-sm font-medium text-primary hover:underline">
      ← Back to products
    </NuxtLink>

    <!-- Loading -->
    <div v-if="pending" class="grid gap-8 sm:grid-cols-2">
      <div class="aspect-square w-full animate-pulse rounded-xl bg-primary-50" />
      <div class="animate-pulse space-y-4">
        <div class="h-6 w-3/4 rounded bg-gray-200" />
        <div class="h-4 w-full rounded bg-gray-200" />
        <div class="h-4 w-2/3 rounded bg-gray-200" />
        <div class="h-6 w-1/4 rounded bg-gray-200" />
      </div>
    </div>

    <!-- Not found -->
    <div
      v-else-if="isNotFound"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't find this product.</p>
      <div class="mt-4 flex justify-center">
        <NuxtLink to="/products">
          <BaseButton variant="secondary">Browse products</BaseButton>
        </NuxtLink>
      </div>
    </div>

    <!-- Error (non-404) -->
    <div
      v-else-if="error"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't load this product right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <!-- Loaded -->
    <div v-else-if="product" class="grid gap-8 sm:grid-cols-2">
      <img
        v-if="imageSrc"
        :src="imageSrc"
        :alt="product.name"
        class="aspect-square w-full rounded-xl object-cover"
      />
      <div
        v-else
        class="flex aspect-square w-full items-center justify-center rounded-xl bg-primary-50 text-6xl font-bold text-primary-300"
        aria-hidden="true"
      >
        {{ initial }}
      </div>

      <div class="space-y-4">
        <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
          {{ product.name }}
        </h1>
        <p class="whitespace-pre-line text-gray-600">{{ product.description }}</p>
        <p class="text-xl font-semibold text-primary">{{ formatPrice(product.price) }}</p>

        <span
          v-if="product.stock <= 0"
          class="inline-block rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-500"
        >
          Out of stock
        </span>

        <template v-else>
          <p class="text-sm text-gray-500">{{ product.stock }} in stock</p>

          <div class="flex items-center gap-3">
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-lg border border-gray-300 text-lg font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="quantity <= 1"
              @click="decrementQuantity"
            >
              −
            </button>
            <span class="w-8 text-center text-lg font-medium">{{ quantity }}</span>
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-lg border border-gray-300 text-lg font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="quantity >= product.stock"
              @click="incrementQuantity"
            >
              +
            </button>
          </div>

          <BaseButton :loading="addingToCart" @click="addToCart">
            {{ auth.isAuthenticated ? "Add to cart" : "Log in to add to cart" }}
          </BaseButton>
        </template>
      </div>
    </div>
  </div>
</template>
