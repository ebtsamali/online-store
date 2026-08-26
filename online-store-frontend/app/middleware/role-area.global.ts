// Global: runs on EVERY route, so it also covers the storefront pages that
// declare no per-page middleware (/, /products, /products/[id]) — which is why
// this rule cannot live in `auth.ts`.
//
// Admin and customer areas are mutually exclusive for an AUTHENTICATED user.
// Logged-out visitors are passed through untouched: the per-page `auth`/`admin`
// guards own those redirects, and the public pages must stay public.
const ADMIN_ROOT = "/admin";
const ADMIN_LOGIN = "/admin/login";
const ADMIN_HOME = "/admin/dashboard";

export default defineNuxtRouteMiddleware((to) => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return;
  }

  // Exact boundary, not a bare startsWith("/admin") — that would also swallow a
  // sibling route such as "/administrators".
  const inAdminArea = to.path === ADMIN_ROOT || to.path.startsWith(ADMIN_ROOT + "/");

  if (auth.isAdmin) {
    // An admin belongs in the admin area, and never on the admin login form.
    if (!inAdminArea || to.path === ADMIN_LOGIN) {
      return navigateTo(ADMIN_HOME, { replace: true });
    }
    return;
  }

  // An authenticated customer never enters the admin area — /admin/login included.
  if (inAdminArea) {
    return navigateTo("/", { replace: true });
  }
});
