// Universal (not .client) so it also runs on the server, where it reads the
// cookie and populates the store BEFORE route middleware executes.
export default defineNuxtPlugin(() => {
  const auth = useAuthStore();
  auth.loadFromStorage();
});
