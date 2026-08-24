<script setup lang="ts">
definePageMeta({ layout: "default", middleware: "auth" });

const { data: orders, pending, error, refresh } = useOrders();

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
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
      Your Orders
    </h1>

    <!-- Loading -->
    <div v-if="pending" class="space-y-4">
      <div v-for="n in 3" :key="n" class="h-16 animate-pulse rounded-lg bg-gray-100" />
    </div>

    <!-- Error -->
    <div
      v-else-if="error"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't load your orders right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <!-- Empty -->
    <div
      v-else-if="orders && orders.length === 0"
      class="rounded-xl border border-gray-200 p-8 text-center text-gray-500"
    >
      <p>You haven't placed any orders yet.</p>
      <div class="mt-4 flex justify-center">
        <NuxtLink to="/products">
          <BaseButton variant="secondary">Browse products</BaseButton>
        </NuxtLink>
      </div>
    </div>

    <!-- Loaded -->
    <div v-else-if="orders" class="space-y-4">
      <NuxtLink
        v-for="order in orders"
        :key="order.id"
        :to="`/orders/${order.id}`"
        class="flex items-center justify-between gap-4 rounded-lg border border-gray-200 p-4 transition hover:shadow-md"
      >
        <div>
          <p class="font-medium text-gray-900">Order #{{ order.id }}</p>
          <p class="text-sm text-gray-500">{{ formatDate(order.createdAt) }}</p>
        </div>
        <div class="text-right">
          <p class="text-sm font-medium text-primary">{{ order.status }}</p>
          <p class="font-semibold text-primary">{{ formatPrice(order.total) }}</p>
        </div>
      </NuxtLink>
    </div>
  </div>
</template>
