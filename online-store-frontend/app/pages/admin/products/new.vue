<script setup lang="ts">
import { toast } from "vue-sonner";
import { productFormSchema, fieldErrors } from "~/utils/validation";

definePageMeta({ layout: "admin", middleware: "admin" });

const apiBase = useApi();
const auth = useAuthStore();

const form = reactive({
  name: "",
  description: "",
  price: "",
  stock: "",
  categoryId: "",
  brandId: "",
});
const imageFile = ref<File | null>(null);
const imageError = ref("");
const errors = ref<Record<string, string>>({});
const loading = ref(false);

const ALLOWED_EXT = [".jpg", ".jpeg", ".png", ".webp"];
const MAX_BYTES = 5 * 1024 * 1024;

function onImageChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0] ?? null;
  imageError.value = "";
  if (!file) {
    imageFile.value = null;
    return;
  }
  const ext = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  if (!ALLOWED_EXT.includes(ext)) {
    imageError.value = "Image must be a .jpg, .jpeg, .png, or .webp file.";
    imageFile.value = null;
    return;
  }
  if (file.size > MAX_BYTES) {
    imageError.value = "Image must be 5 MB or smaller.";
    imageFile.value = null;
    return;
  }
  imageFile.value = file;
}

async function onSubmit() {
  errors.value = {};

  const parsed = productFormSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }
  if (!imageFile.value) {
    imageError.value = "Image is required.";
    return;
  }

  const body = new FormData();
  body.append("Name", parsed.data.name);
  body.append("Description", parsed.data.description);
  body.append("Price", String(parsed.data.price));
  body.append("Stock", String(parsed.data.stock));
  body.append("CategoryId", String(parsed.data.categoryId));
  body.append("BrandId", String(parsed.data.brandId));
  body.append("Image", imageFile.value);

  loading.value = true;
  try {
    await $fetch(`${apiBase}/products`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body,
    });
    toast.success(`"${parsed.data.name}" created.`);
    await navigateTo("/admin/products");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not create product.");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-xl space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Add product</h1>

    <BaseForm @submit="onSubmit">
      <BaseInput v-model="form.name" label="Name" :error="errors.name" />

      <div class="space-y-1.5">
        <label class="block text-sm font-medium text-gray-700">Description</label>
        <textarea
          v-model="form.description"
          rows="4"
          class="w-full rounded-lg border border-gray-300 px-4 py-3 text-sm text-gray-900 focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary"
        />
      </div>

      <BaseInput
        v-model="form.price"
        type="number"
        step="0.01"
        label="Price"
        :error="errors.price"
      />
      <BaseInput v-model="form.stock" type="number" label="Stock" :error="errors.stock" />
      <BaseInput
        v-model="form.categoryId"
        type="number"
        label="Category ID"
        :error="errors.categoryId"
      />
      <BaseInput
        v-model="form.brandId"
        type="number"
        label="Brand ID"
        :error="errors.brandId"
      />

      <div class="space-y-1.5">
        <label class="block text-sm font-medium text-gray-700">Image</label>
        <input
          type="file"
          accept=".jpg,.jpeg,.png,.webp"
          class="block w-full text-sm"
          @change="onImageChange"
        />
        <p v-if="imageError" class="text-xs text-red-600">{{ imageError }}</p>
      </div>

      <BaseButton type="submit" :loading="loading">Create product</BaseButton>
    </BaseForm>
  </div>
</template>
