<script setup lang="ts">
import { toast } from "vue-sonner";
import type { PagedProducts, ProductListItem } from "~/composables/useProducts";

definePageMeta({ layout: "admin", middleware: "admin" });

const route = useRoute();
const router = useRouter();
const apiBase = useApi();
const auth = useAuthStore();

const page = ref(Number(route.query.page) || 1);
const searchInput = ref(String(route.query.search || ""));
const search = ref(String(route.query.search || ""));
const pageSize = 20;

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
  `${apiBase}/products`,
  {
    key: "admin-products-list",
    query: computed(() => ({
      page: page.value,
      pageSize,
      search: search.value || undefined,
    })),
    headers: { Authorization: `Bearer ${auth.token}` },
    server: false,
    watch: [page, search],
    default: (): PagedProducts => ({ items: [], page: page.value, pageSize, total: 0 }),
  },
);

const hasPrev = computed(() => page.value > 1);
const hasNext = computed(() => page.value * pageSize < (data.value?.total ?? 0));

async function onDeleteRequest(product: ProductListItem) {
  if (!confirm(`Delete "${product.name}"? This cannot be undone.`)) return;

  try {
    await $fetch(`${apiBase}/products/${product.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success(`"${product.name}" deleted.`);
    refresh();
  } catch (e: any) {
    toast.error(
      `Could not delete "${product.name}". If it appears in any order, deactivate it instead — edit the product and turn off "Active".`,
    );
  }
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Products</h1>
        <input
          v-model="searchInput"
          type="search"
          placeholder="Search products..."
          class="mt-3 w-full max-w-sm rounded-lg border border-gray-300 px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary"
        />
      </div>
      <NuxtLink to="/admin/products/new">
        <BaseButton>Add product</BaseButton>
      </NuxtLink>
    </div>

    <div
      v-if="error"
      class="rounded-xl border border-gray-200 p-8 text-center"
    >
      <p class="text-gray-700">We couldn't load products right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <template v-else>
      <AdminRecentProductsTable
        :products="data?.items ?? []"
        :loading="pending"
        @delete="onDeleteRequest"
      />

      <div
        v-if="(data?.total ?? 0) > 0"
        class="flex items-center justify-center gap-4"
      >
        <BaseButton
          variant="secondary"
          :disabled="!hasPrev"
          @click="page = Math.max(1, page - 1)"
        >
          Previous
        </BaseButton>
        <span class="text-sm text-gray-500">
          Page {{ page }} of {{ Math.max(1, Math.ceil((data?.total ?? 0) / pageSize)) }}
        </span>
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
