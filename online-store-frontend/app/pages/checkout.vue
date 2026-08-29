<script setup lang="ts">
import { toast } from "vue-sonner";

definePageMeta({ layout: "default", middleware: "auth" });

const apiBase = useApi();
const auth = useAuthStore();

const { data: cart, pending, error, refresh } = useCart();

const placingOrder = ref(false);

const shipping = reactive({
  name: "",
  region: "",
  city: "",
  phone: "",
});

const shippingErrors = reactive({
  name: "",
  region: "",
  city: "",
  phone: "",
});

watchEffect(() => {
  if (cart.value && cart.value.items.length === 0) {
    navigateTo("/cart");
  }
});

function validateShipping(): boolean {
  let valid = true;
  shippingErrors.name = shipping.name.trim() ? "" : "Name is required";
  shippingErrors.region = shipping.region.trim() ? "" : "Region is required";
  shippingErrors.city = shipping.city.trim() ? "" : "City is required";
  shippingErrors.phone = shipping.phone.trim() ? "" : "Phone number is required";
  if (shippingErrors.name || shippingErrors.region || shippingErrors.city || shippingErrors.phone) {
    valid = false;
  }
  return valid;
}

async function placeOrder() {
  if (!validateShipping()) return;

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
      body: {
        shippingName: shipping.name.trim(),
        shippingRegion: shipping.region.trim(),
        shippingCity: shipping.city.trim(),
        shippingPhone: shipping.phone.trim(),
      },
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
      Checkout
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

    <!-- Loaded -->
    <div v-else-if="cart" class="grid gap-8 lg:grid-cols-2">
      <!-- Left: Shipping form -->
      <div class="space-y-6">
        <h2 class="text-lg font-semibold text-gray-900">Shipping Information</h2>

        <div class="space-y-4">
          <!-- Name -->
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700">Full Name</label>
            <input
              v-model="shipping.name"
              type="text"
              placeholder="e.g. Ahmed Mohammed"
              class="w-full rounded-lg border px-3 py-2 text-sm shadow-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              :class="shippingErrors.name ? 'border-red-400' : 'border-gray-300'"
            />
            <p v-if="shippingErrors.name" class="mt-1 text-xs text-red-500">{{ shippingErrors.name }}</p>
          </div>

          <!-- Region -->
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700">Region</label>
            <input
              v-model="shipping.region"
              type="text"
              placeholder="e.g. Riyadh Region"
              class="w-full rounded-lg border px-3 py-2 text-sm shadow-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              :class="shippingErrors.region ? 'border-red-400' : 'border-gray-300'"
            />
            <p v-if="shippingErrors.region" class="mt-1 text-xs text-red-500">{{ shippingErrors.region }}</p>
          </div>

          <!-- City -->
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700">City</label>
            <input
              v-model="shipping.city"
              type="text"
              placeholder="e.g. Riyadh"
              class="w-full rounded-lg border px-3 py-2 text-sm shadow-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              :class="shippingErrors.city ? 'border-red-400' : 'border-gray-300'"
            />
            <p v-if="shippingErrors.city" class="mt-1 text-xs text-red-500">{{ shippingErrors.city }}</p>
          </div>

          <!-- Phone -->
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700">Phone Number</label>
            <input
              v-model="shipping.phone"
              type="tel"
              placeholder="e.g. +966 5x xxx xxxx"
              class="w-full rounded-lg border px-3 py-2 text-sm shadow-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              :class="shippingErrors.phone ? 'border-red-400' : 'border-gray-300'"
            />
            <p v-if="shippingErrors.phone" class="mt-1 text-xs text-red-500">{{ shippingErrors.phone }}</p>
          </div>
        </div>
      </div>

      <!-- Right: Order summary -->
      <div class="space-y-6">
        <h2 class="text-lg font-semibold text-gray-900">Order Summary</h2>

        <div class="space-y-3">
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

        <div class="rounded-lg bg-gray-50 px-4 py-3">
          <p class="text-lg font-semibold">Total: {{ formatPrice(cart.total) }}</p>
        </div>

        <p class="text-sm text-gray-500">
          Simulated payment — no real charge will be made.
        </p>

        <BaseButton block :loading="placingOrder" @click="placeOrder">
          {{ placingOrder ? "Placing order…" : "Place order" }}
        </BaseButton>
      </div>
    </div>
  </div>
</template>
