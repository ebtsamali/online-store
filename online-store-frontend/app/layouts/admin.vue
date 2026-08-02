<script setup lang="ts">
const auth = useAuthStore();
</script>

<template>
  <div class="min-h-screen flex">
    <aside class="w-60 shrink-0 bg-gray-900 text-gray-100 flex flex-col">
      <div class="px-4 py-4 text-lg font-bold border-b border-gray-700">Admin</div>
      <nav class="flex-1 flex flex-col p-2 gap-1 text-sm">
        <NuxtLink to="/admin/dashboard" class="px-3 py-2 rounded hover:bg-gray-800">Dashboard</NuxtLink>
        <NuxtLink to="/admin/products" class="px-3 py-2 rounded hover:bg-gray-800">Products</NuxtLink>
      </nav>
      <div class="p-2 border-t border-gray-700">
        <button class="w-full text-left px-3 py-2 rounded hover:bg-gray-800" @click="auth.logout()">
          Log out
        </button>
      </div>
    </aside>

    <div class="flex-1 flex flex-col">
      <header class="border-b px-6 py-4 text-sm text-gray-600">
        Signed in as {{ auth.user?.email }} ({{ auth.user?.role }})
      </header>
      <main class="flex-1 p-6">
        <slot />
      </main>
    </div>
  </div>
</template>
