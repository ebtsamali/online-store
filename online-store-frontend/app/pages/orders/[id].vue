<script setup lang="ts">
definePageMeta({ layout: "default", middleware: "auth" });

const route = useRoute();

const rawId = Number(route.params.id);
// A non-numeric or non-positive id can never match the backend's
// `[HttpGet("{id:int}")]` route — treat it as "not found" without a request.
const isValidId = Number.isInteger(rawId) && rawId > 0;

const { data: order, pending, error, refresh } = isValidId
  ? useOrder(rawId)
  : { data: ref(null), pending: ref(false), error: ref({ statusCode: 404 }), refresh: () => {} };

// Both "order not found" (404) and "another user's order" (403, bare Forbid
// with no JSON body) render the identical not-found UI — see Context item 10.
const isNotFound = computed(() => {
  const code = (error.value as any)?.statusCode;
  return code === 404 || code === 403;
});

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
</script>

<template>
  <div class="space-y-8">
    <NuxtLink to="/orders" class="text-sm font-medium text-primary hover:underline">
      ← Back to orders
    </NuxtLink>

    <!-- Loading -->
    <div v-if="pending" class="animate-pulse space-y-4">
      <div class="h-6 w-1/3 rounded bg-gray-200" />
      <div v-for="n in 3" :key="n" class="h-16 rounded-lg bg-gray-100" />
    </div>

    <!-- Not found -->
    <div
      v-else-if="isNotFound"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't find this order.</p>
      <div class="mt-4 flex justify-center">
        <NuxtLink to="/orders">
          <BaseButton variant="secondary">Back to orders</BaseButton>
        </NuxtLink>
      </div>
    </div>

    <!-- Error (non-404/403) -->
    <div
      v-else-if="error"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't load this order right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <!-- Loaded -->
    <div v-else-if="order" class="space-y-6">
      <div>
        <div class="flex items-center gap-3">
          <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
            Order #{{ order.id }}
          </h1>
          <span class="rounded-full bg-primary-50 px-2.5 py-1 text-xs font-medium text-primary">
            {{ order.status }}
          </span>
        </div>
        <p class="mt-1 text-sm text-gray-500">{{ formatDate(order.createdAt) }}</p>
      </div>

      <!-- Shipping info -->
      <div class="rounded-lg border border-gray-200 p-4">
        <h2 class="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-500">Shipping Address</h2>
        <div class="space-y-1 text-sm text-gray-700">
          <p><span class="font-medium">Name:</span> {{ order.shippingName }}</p>
          <p><span class="font-medium">Region:</span> {{ order.shippingRegion }}</p>
          <p><span class="font-medium">City:</span> {{ order.shippingCity }}</p>
          <p><span class="font-medium">Phone:</span> {{ order.shippingPhone }}</p>
        </div>
      </div>

      <div class="space-y-4">
        <div
          v-for="item in order.items"
          :key="item.productId"
          class="flex items-center justify-between gap-4 rounded-lg border border-gray-200 p-4"
        >
          <div>
            <p class="font-medium text-gray-900">{{ item.productName }}</p>
            <p class="text-sm text-gray-500">
              {{ formatPrice(item.unitPrice) }} × {{ item.quantity }}
            </p>
          </div>
          <p class="font-semibold text-primary">{{ formatPrice(item.subtotal) }}</p>
        </div>
      </div>

      <p class="text-lg font-semibold">Total: {{ formatPrice(order.total) }}</p>
    </div>
  </div>
</template>
