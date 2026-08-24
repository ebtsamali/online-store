<script setup lang="ts">
import type { PagedProducts } from "~/composables/useProducts";

const route = useRoute();
const router = useRouter();

const page = ref(Number(route.query.page) || 1);
const searchInput = ref(String(route.query.search || ""));
const search = ref(String(route.query.search || ""));
const pageSize = 12;

let debounceTimer: ReturnType<typeof setTimeout> | undefined;
watch(searchInput, (value) => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    search.value = value;
    page.value = 1;
  }, 400);
});

watch([page, search], ([newPage, newSearch]) => {
  router.push({
    query: {
      ...(newPage > 1 ? { page: newPage } : {}),
      ...(newSearch ? { search: newSearch } : {}),
    },
  });
});

const { data, pending, error, refresh } = useFetch<PagedProducts>(
  `${useApi()}/products`,
  {
    key: "products-catalog",
    query: computed(() => ({
      page: page.value,
      pageSize,
      search: search.value || undefined,
    })),
    server: false,
    watch: [page, search],
    default: (): PagedProducts => ({ items: [], page: page.value, pageSize, total: 0 }),
  },
);

const totalPages = computed(() => Math.max(1, Math.ceil((data.value?.total ?? 0) / pageSize)));
const hasPrev = computed(() => page.value > 1);
const hasNext = computed(() => page.value * pageSize < (data.value?.total ?? 0));
</script>

<template>
  <div class="space-y-8">
    <div>
      <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
        Products
      </h1>
      <input
        v-model="searchInput"
        type="search"
        placeholder="Search products..."
        class="mt-4 w-full max-w-md rounded-lg border border-gray-300 px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary sm:w-80"
      />
    </div>

    <!-- Loading -->
    <div v-if="pending" class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
      <div
        v-for="n in pageSize"
        :key="n"
        class="animate-pulse overflow-hidden rounded-xl border border-gray-200"
      >
        <div class="aspect-square w-full bg-primary-50" />
        <div class="space-y-3 p-4">
          <div class="h-4 w-3/4 rounded bg-gray-200" />
          <div class="h-4 w-1/3 rounded bg-gray-200" />
        </div>
      </div>
    </div>

    <!-- Error -->
    <div
      v-else-if="error"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't load products right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <!-- Empty -->
    <div
      v-else-if="data && data.items.length === 0"
      class="rounded-xl border border-gray-200 p-8 text-center text-gray-500"
    >
      No products found. Try a different search.
    </div>

    <!-- Loaded -->
    <template v-else>
      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <ProductCard
          v-for="product in data?.items ?? []"
          :key="product.id"
          :product="product"
        />
      </div>

      <div
        v-if="data && data.total > 0"
        class="flex items-center justify-center gap-4"
      >
        <BaseButton
          variant="secondary"
          :disabled="!hasPrev"
          @click="page = Math.max(1, page - 1)"
        >
          Previous
        </BaseButton>
        <span class="text-sm text-gray-500">Page {{ page }} of {{ totalPages }}</span>
        <BaseButton
          variant="secondary"
          :disabled="!hasNext"
          @click="page = page + 1"
        >
          Next
        </BaseButton>
      </div>
    </template>
  </div>
</template>
