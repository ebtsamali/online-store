<script setup lang="ts">
import { toast } from "vue-sonner";
import { registerSchema, fieldErrors } from "~/utils/validation";

definePageMeta({ layout: false, middleware: "guest" });

const apiBase = useApi();

const form = reactive({ name: "", email: "", password: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

async function onSubmit() {
  errors.value = {};

  const parsed = registerSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    await $fetch(`${apiBase}/auth/register`, {
      method: "POST",
      body: parsed.data,
    });
    // Register does NOT return a token — send the user to log in.
    toast.success("Account created — please log in");
    await navigateTo("/auth/login");
  } catch (e: any) {
    // 409 Conflict → "Email already exists".
    toast.error(e?.data?.message ?? "Registration failed");
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

      <h1 class="mt-2 text-center text-3xl font-bold text-[#1b3a6b]">
        Create account
      </h1>

      <BaseForm class="mt-8" @submit="onSubmit">
        <BaseInput
          v-model="form.name"
          label="Full name"
          autocomplete="name"
          placeholder="Your name"
          :error="errors.name"
        />

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
          autocomplete="new-password"
          placeholder="••••••••"
          :error="errors.password"
        />

        <BaseButton type="submit" block :loading="loading">
          {{ loading ? "Creating…" : "Register" }}
        </BaseButton>
      </BaseForm>

      <p class="mt-6 text-center text-sm text-[#8a94a6]">
        Have an account?
        <NuxtLink to="/auth/login" class="font-semibold text-[#1b3a6b]">
          Log in
        </NuxtLink>
      </p>
    </div>
  </div>
</template>
