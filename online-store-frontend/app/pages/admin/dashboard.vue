<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

const auth = useAuthStore();

// Two fetches with distinct keys: 100 (the API's pageSize ceiling) for the
// derived counts, 5 for the table. A shared key would make useFetch hand back
// the other call's payload with the wrong pageSize.
const {
  data: statsData,
  pending: statsPending,
  error: statsError,
  refresh: refreshStats,
} = useProducts(1, 100, "admin-product-stats");

const {
  data: recentData,
  pending: recentPending,
  error: recentError,
  refresh: refreshRecent,
} = useProducts(1, 5, "admin-recent-products");

const totalProducts = computed(() => statsData.value?.total ?? 0);
const activeCount = computed(
  () => statsData.value?.items.filter((p) => p.isActive).length ?? 0,
);
const outOfStockCount = computed(
  () => statsData.value?.items.filter((p) => p.stock <= 0).length ?? 0,
);
const recentProducts = computed(() => recentData.value?.items ?? []);
const hasError = computed(() => !!statsError.value || !!recentError.value);

function retry() {
  refreshStats();
  refreshRecent();
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-xl font-semibold text-gray-900">Dashboard</h1>
      <p class="text-sm text-gray-500">
        Welcome back, {{ auth.user?.name ?? "Admin" }}.
      </p>
    </div>

    <!-- One combined notice, even when both fetches fail. -->
    <div
      v-if="hasError"
      class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-gray-200 bg-white p-4"
    >
      <p class="text-sm text-gray-700">Couldn't load product data.</p>
      <BaseButton variant="secondary" @click="retry()">Retry</BaseButton>
    </div>

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <AdminStatCard
        label="Total products"
        :value="totalProducts"
        hint="active products"
        :loading="statsPending"
      />
      <AdminStatCard
        label="Active products"
        :value="activeCount"
        hint="of first 100"
        :loading="statsPending"
      />
      <AdminStatCard
        label="Out of stock"
        :value="outOfStockCount"
        hint="of first 100"
        :loading="statsPending"
      />
      <!-- Follow-up: no admin stats endpoint exists in OnlineStore.API, so order
           and revenue figures are deliberately absent rather than fabricated. -->
      <div
        class="flex items-center rounded-xl border border-dashed border-gray-300 p-4 text-xs text-gray-400"
      >
        Orders &amp; revenue — pending stats endpoint
      </div>
    </div>

    <div class="flex items-center justify-between">
      <h2 class="text-base font-semibold text-gray-900">Recent products</h2>
      <NuxtLink
        to="/admin/products"
        class="text-sm font-medium text-primary hover:underline"
      >
        Manage products
      </NuxtLink>
    </div>

    <AdminRecentProductsTable
      :products="recentProducts"
      :loading="recentPending"
    />
  </div>
</template>
