<script setup lang="ts">
import { toast } from "vue-sonner";
import { loginSchema, fieldErrors } from "~/utils/validation";

definePageMeta({ layout: false });

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

    auth.login(res.token, { name: res.name, email: res.email, role: res.role });
    toast.success(`Welcome back, ${res.name}`);
    await navigateTo("/");
  } catch (e: any) {
    // Backend returns a generic 401 "Invalid email or password".
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

      <h1 class="mt-2 text-center text-3xl font-bold text-[#1b3a6b]">Log in</h1>

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
        >
          <template #label-aside>
            <span class="text-xs text-[#8a94a6]">Forgot password?</span>
          </template>
        </BaseInput>

        <BaseButton type="submit" block :loading="loading">
          {{ loading ? "Signing in…" : "Log in" }}
        </BaseButton>
      </BaseForm>

      <p class="mt-6 text-center text-sm text-[#8a94a6]">
        No account?
        <NuxtLink to="/auth/register" class="font-semibold text-[#1b3a6b]">
          Register
        </NuxtLink>
      </p>
    </div>
  </div>
</template>
