<script setup lang="ts">
const { data, pending, error, refresh } = useProducts(1, 8, "home-new-arrivals");

// Static until the API exposes a categories endpoint.
const categories = [
  { label: "Electronics", initial: "E" },
  { label: "Home & Living", initial: "H" },
  { label: "Fashion", initial: "F" },
  { label: "Accessories", initial: "A" },
];
</script>

<template>
  <div class="space-y-16 sm:space-y-24">
    <!-- Hero -->
    <section class="rounded-2xl bg-primary px-6 py-16 text-white sm:px-12 sm:py-24">
      <div class="mx-auto max-w-3xl text-center">
        <p class="text-sm font-semibold uppercase tracking-widest text-primary-200">
          Fresh stock, every week
        </p>
        <h1 class="mt-4 text-4xl font-bold tracking-tight sm:text-5xl">
          Everything you need, in one store
        </h1>
        <p class="mx-auto mt-6 max-w-2xl text-lg text-primary-100">
          Browse a curated catalog of everyday essentials and new finds, with
          straightforward pricing and fast checkout.
        </p>
        <div class="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
          <NuxtLink to="/products">
            <BaseButton variant="secondary">Shop now</BaseButton>
          </NuxtLink>
          <a href="#new-arrivals">
            <BaseButton variant="ghost" class="text-white hover:bg-primary-600">
              Browse new arrivals
            </BaseButton>
          </a>
        </div>
      </div>
    </section>

    <!-- New arrivals -->
    <section id="new-arrivals">
      <div class="mb-8">
        <h2 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
          New arrivals
        </h2>
        <p class="mt-2 text-gray-500">
          The latest products added to the catalog, newest first.
        </p>
      </div>

      <!-- Loading -->
      <div v-if="pending" class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div
          v-for="n in 8"
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
        No products yet — check back soon.
      </div>

      <!-- Loaded -->
      <div v-else class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <ProductCard
          v-for="product in data?.items ?? []"
          :key="product.id"
          :product="product"
        />
      </div>

      <div class="mt-8">
        <NuxtLink to="/products" class="font-semibold text-primary hover:underline">
          View all products →
        </NuxtLink>
      </div>
    </section>

    <!-- Category teaser — tiles are static pending a categories endpoint on the API. -->
    <section>
      <h2 class="mb-8 text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
        Shop by category
      </h2>
      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <NuxtLink
          v-for="category in categories"
          :key="category.label"
          to="/products"
          class="flex items-center gap-4 rounded-xl bg-primary-50 p-6 transition hover:bg-primary-100"
        >
          <span
            class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary text-lg font-bold text-white"
            aria-hidden="true"
          >
            {{ category.initial }}
          </span>
          <span class="font-medium text-primary">{{ category.label }}</span>
        </NuxtLink>
      </div>
    </section>

    <!-- Closing CTA -->
    <section class="rounded-2xl bg-primary-50 px-6 py-16 text-center">
      <h2 class="text-2xl font-bold tracking-tight text-primary sm:text-3xl">
        Ready to find your next favourite?
      </h2>
      <p class="mx-auto mt-4 max-w-xl text-gray-600">
        The full catalog is a click away — new items land every week.
      </p>
      <div class="mt-8 flex justify-center">
        <NuxtLink to="/products">
          <BaseButton>Browse full catalog</BaseButton>
        </NuxtLink>
      </div>
    </section>
  </div>
</template>
