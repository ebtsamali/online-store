import { z } from "zod";

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
});

export const registerSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z
    .string()
    .min(1, "Email is required")
    .email("Enter a valid email address"),
  password: z.string().min(6, "Password must be at least 6 characters"),
});

export const productFormSchema = z.object({
  name: z.string().min(1, "Name is required").max(100, "Name must be 100 characters or fewer"),
  description: z.string(),
  price: z.coerce.number({ invalid_type_error: "Price must be a number" }).gt(0, "Price must be greater than 0"),
  stock: z.coerce.number({ invalid_type_error: "Stock must be a number" }).int("Stock must be a whole number").min(0, "Stock must be 0 or greater"),
  categoryId: z.coerce.number({ invalid_type_error: "Category id must be a number" }).int().min(1, "Category id is required"),
  brandId: z.coerce.number({ invalid_type_error: "Brand id must be a number" }).int().min(1, "Brand id is required"),
});

export const nameFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Name is required")
    .max(100, "Name must be 100 characters or fewer"),
});

export type LoginInput = z.infer<typeof loginSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;
export type ProductFormInput = z.infer<typeof productFormSchema>;
export type NameFormInput = z.infer<typeof nameFormSchema>;

// Flattens a zod error into { field: firstMessage } for BaseInput's `error` prop.
// First message wins — the design shows a single line under each input.
export function fieldErrors(error: z.ZodError): Record<string, string> {
  const out: Record<string, string> = {};
  for (const issue of error.issues) {
    const key = String(issue.path[0] ?? "");
    if (key && !(key in out)) {
      out[key] = issue.message;
    }
  }
  return out;
}
