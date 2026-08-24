<script setup lang="ts">
const auth = useAuthStore();
</script>

<template>
  <div class="min-h-screen flex">
    <aside class="w-60 shrink-0 bg-gray-900 text-gray-100 flex flex-col">
      <div class="px-4 py-4 text-lg font-bold border-b border-gray-700">Admin</div>
      <nav class="flex-1 flex flex-col p-2 gap-1 text-sm">
        <NuxtLink
          to="/admin/dashboard"
          class="px-3 py-2 rounded hover:bg-gray-800"
          active-class="bg-primary text-white hover:bg-primary-600"
        >
          Dashboard
        </NuxtLink>
        <!-- active-class, not exact-active-class: /admin/products/3 must keep this highlighted. -->
        <NuxtLink
          to="/admin/products"
          class="px-3 py-2 rounded hover:bg-gray-800"
          active-class="bg-primary text-white hover:bg-primary-600"
        >
          Products
        </NuxtLink>
        <NuxtLink
          to="/admin/categories"
          class="px-3 py-2 rounded hover:bg-gray-800"
          active-class="bg-primary text-white hover:bg-primary-600"
        >
          Categories
        </NuxtLink>
        <NuxtLink
          to="/admin/brands"
          class="px-3 py-2 rounded hover:bg-gray-800"
          active-class="bg-primary text-white hover:bg-primary-600"
        >
          Brands
        </NuxtLink>
      </nav>
      <div class="p-2 border-t border-gray-700">
        <button class="w-full text-left px-3 py-2 rounded hover:bg-gray-800" @click="auth.logout()">
          Log out
        </button>
      </div>
    </aside>

    <div class="flex-1 flex flex-col">
      <header class="border-b bg-white px-6 py-3 flex items-center justify-between">
        <div class="flex flex-col leading-tight">
          <span class="text-sm font-semibold text-gray-900">{{ auth.user?.name ?? "Admin" }}</span>
          <span class="text-xs text-gray-500">{{ auth.user?.email }}</span>
        </div>
        <div class="flex items-center gap-3">
          <span
            class="rounded-full bg-primary-50 px-2.5 py-1 text-xs font-medium uppercase tracking-wide text-primary"
          >
            {{ auth.user?.role }}
          </span>
          <button class="text-sm text-gray-600 hover:text-primary" @click="auth.logout()">
            Log out
          </button>
        </div>
      </header>
      <main class="flex-1 p-6">
        <slot />
      </main>
    </div>
  </div>
</template>
