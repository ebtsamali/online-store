export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return navigateTo("/admin/login");
  }
  if (!auth.isAdmin) {
    // Authenticated but not an admin → send to the customer home, NOT the dashboard.
    return navigateTo("/");
  }
});
