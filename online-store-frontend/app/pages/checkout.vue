<script setup lang="ts">
import { toast } from "vue-sonner";

definePageMeta({ layout: "default", middleware: "auth" });

const apiBase = useApi();
const auth = useAuthStore();

const { data: cart, pending, error, refresh } = useCart();

const placingOrder = ref(false);

watchEffect(() => {
  if (cart.value && cart.value.items.length === 0) {
    navigateTo("/cart");
  }
});

async function placeOrder() {
  placingOrder.value = true;
  try {
    const order = await $fetch<{
      id: number;
      status: string;
      total: number;
      createdAt: string;
      items: {
        productId: number;
        productName: string;
        unitPrice: number;
        quantity: number;
        subtotal: number;
      }[];
    }>(`${apiBase}/orders/checkout`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success("Order placed successfully");
    await navigateTo(`/orders/${order.id}`);
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not place your order");
  } finally {
    placingOrder.value = false;
  }
}
</script>

<template>
  <div class="space-y-8">
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl">
      Review Your Order
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

    <!-- Redirecting (empty cart) -->
    <div v-else-if="cart && cart.items.length === 0" class="text-gray-500">
      Your cart is empty — redirecting…
    </div>

    <!-- Loaded (review + confirm) -->
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
          <p class="font-semibold text-primary">{{ formatPrice(item.subtotal) }}</p>
        </div>
      </div>

      <p class="text-lg font-semibold">Total: {{ formatPrice(cart.total) }}</p>

      <p class="text-sm text-gray-500">
        Simulated payment — no real charge will be made.
      </p>

      <BaseButton block :loading="placingOrder" @click="placeOrder">
        {{ placingOrder ? "Placing order…" : "Place order" }}
      </BaseButton>
    </div>
  </div>
</template>
