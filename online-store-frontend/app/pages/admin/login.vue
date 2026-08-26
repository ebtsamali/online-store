<script setup lang="ts">
import { toast } from "vue-sonner";
import { loginSchema, fieldErrors } from "~/utils/validation";

definePageMeta({ layout: false, middleware: "guest" });

const auth = useAuthStore();
const apiBase = useApi();

const form = reactive({ email: "", password: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

async function onSubmit() {
  errors.value = {};

  const parsed = loginSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    const res = await $fetch<{
      token: string;
      name: string;
      email: string;
      role: string;
    }>(`${apiBase}/auth/login`, { method: "POST", body: parsed.data });

    if (res.role !== "admin") {
      toast.error("This sign-in is for admins only.");
      return;
    }

    auth.login(res.token, { name: res.name, email: res.email, role: res.role });
    toast.success(`Welcome back, ${res.name}`);
    await navigateTo("/admin/dashboard");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Invalid email or password");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen bg-[#f5f6fa] flex items-center justify-center p-4">
    <div class="w-full max-w-md rounded-2xl bg-white p-8 shadow-lg sm:p-10">
      <div class="flex justify-center">
        <NuxtLink to="/">
          <img
            src="/images/logo.jpg"
            alt="Online Store"
            class="h-[128px] w-auto object-contain"
          />
        </NuxtLink>
      </div>

      <h1 class="mt-2 text-center text-3xl font-bold text-[#1b3a6b]">Admin sign in</h1>
      <p class="mt-1 text-center text-sm text-[#8a94a6]">Staff access only</p>

      <BaseForm class="mt-8" @submit="onSubmit">
        <BaseInput
          v-model="form.email"
          label="Email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          :error="errors.email"
        />

        <BaseInput
          v-model="form.password"
          label="Password"
          type="password"
          autocomplete="current-password"
          placeholder="••••••••"
          :error="errors.password"
        />

        <BaseButton type="submit" block :loading="loading">
          {{ loading ? "Signing in…" : "Sign in" }}
        </BaseButton>
      </BaseForm>

      <p class="mt-6 text-center text-sm text-[#8a94a6]">
        <NuxtLink to="/" class="font-semibold text-[#1b3a6b]">Back to store</NuxtLink>
      </p>
    </div>
  </div>
</template>
