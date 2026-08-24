<script setup lang="ts">
const auth = useAuthStore();
</script>

<template>
  <div class="min-h-screen flex flex-col">
    <header class="border-b px-6 py-4 flex items-center justify-between">
      <NuxtLink to="/" class="text-xl font-bold text-primary">Online Store</NuxtLink>
      <nav class="flex items-center gap-4 text-sm">
        <NuxtLink to="/products">Products</NuxtLink>
        <NuxtLink to="/cart">Cart</NuxtLink>
        <NuxtLink v-if="auth.isAuthenticated" to="/orders">Orders</NuxtLink>
        <NuxtLink v-if="!auth.isAuthenticated" to="/auth/login">Log in</NuxtLink>
        <button v-else class="text-red-600" @click="auth.logout()">Log out</button>
      </nav>
    </header>

    <main class="flex-1 p-6">
      <slot />
    </main>

    <footer class="border-t px-6 py-4 text-center text-sm text-gray-500">
      © Online Store
    </footer>
  </div>
</template>
