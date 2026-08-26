// Public-only pages (login / register). An authenticated visitor is sent to the
// home appropriate to their role instead of being shown a sign-in form again.
// `replace: true` overwrites the auth-page history entry, so pressing Back after
// signing in cannot bounce the user between the form and their home.
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return;
  }
  return navigateTo(auth.isAdmin ? "/admin/dashboard" : "/", { replace: true });
});
