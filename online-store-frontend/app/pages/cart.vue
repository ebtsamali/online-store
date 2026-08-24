<script setup lang="ts">
import { toast } from "vue-sonner";

definePageMeta({ middleware: "auth" }); // default layout (no explicit layout)

const apiBase = useApi();
const auth = useAuthStore();

const { data: cart, pending, error, refresh } = useCart();

const removingId = ref<number | null>(null);

async function removeItem(id: number) {
  removingId.value = id;
  try {
    await $fetch(`${apiBase}/cart/${id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success("Item removed from cart");
    await refresh();
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not remove this item");
  } finally {
    removingId.value = null;
  }
}
</script>

<template>
  <div class="space-y-8">
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
      Your Cart
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
      <p class="text-gray-700">We couldn't load your cart right now.</p>
      <div class="mt-4 flex justify-center">
        <BaseButton variant="secondary" @click="refresh()">Retry</BaseButton>
      </div>
    </div>

    <!-- Empty -->
    <div
      v-else-if="cart && cart.items.length === 0"
      class="rounded-xl border border-gray-200 p-8 text-center text-gray-500"
    >
      <p>Your cart is empty.</p>
      <div class="mt-4 flex justify-center">
        <NuxtLink to="/products">
          <BaseButton variant="secondary">Browse products</BaseButton>
        </NuxtLink>
      </div>
    </div>

    <!-- Loaded -->
    <div v-else-if="cart" class="space-y-6">
      <div class="space-y-4">
        <div
          v-for="item in cart.items"
          :key="item.id"
          class="flex items-center justify-between gap-4 rounded-lg border border-gray-200 p-4"
        >
          <div>
            <p class="font-medium text-gray-900">{{ item.productName }}</p>
            <p class="text-sm text-gray-500">
              {{ formatPrice(item.price) }} × {{ item.quantity }}
            </p>
          </div>
          <div class="flex items-center gap-4">
            <p class="font-semibold text-primary">{{ formatPrice(item.subtotal) }}</p>
            <BaseButton
              variant="secondary"
              :loading="removingId === item.id"
              @click="removeItem(item.id)"
            >
              Remove
            </BaseButton>
          </div>
        </div>
      </div>

      <p class="text-lg font-semibold">Total: {{ formatPrice(cart.total) }}</p>

      <NuxtLink to="/checkout">
        <BaseButton :disabled="cart.items.length === 0">Proceed to checkout</BaseButton>
      </NuxtLink>
    </div>
  </div>
</template>
