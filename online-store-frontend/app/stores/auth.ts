export interface AuthUser {
  name: string;
  email: string;
  role: string;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
}

// One cookie holds the whole auth payload so BOTH token and role are
// readable during SSR (required by the route guards).
// useCookie serialises objects to JSON automatically.
function authCookie() {
  return useCookie<{ token: string; user: AuthUser } | null>("auth", {
    maxAge: 60 * 60 * 24 * 7, // 7 days
    sameSite: "lax",
    path: "/",
  });
}

export const useAuthStore = defineStore("auth", {
  state: (): AuthState => ({ token: null, user: null }),

  getters: {
    isAuthenticated: (state): boolean => !!state.token,
    isAdmin: (state): boolean => state.user?.role === "admin",
  },

  actions: {
    login(token: string, user: AuthUser) {
      this.token = token;
      this.user = user;
      authCookie().value = { token, user };
      if (import.meta.client) {
        localStorage.setItem("auth", JSON.stringify({ token, user }));
      }
    },

    logout() {
      const wasAdmin = this.isAdmin;
      this.token = null;
      this.user = null;
      authCookie().value = null;
      if (import.meta.client) {
        localStorage.removeItem("auth");
      }
      return navigateTo(wasAdmin ? "/admin/login" : "/auth/login");
    },

    // Runs on server (cookie) and client (cookie, then localStorage fallback).
    loadFromStorage() {
      const cookie = authCookie();
      if (cookie.value) {
        this.token = cookie.value.token;
        this.user = cookie.value.user;
        return;
      }
      if (import.meta.client) {
        const raw = localStorage.getItem("auth");
        if (raw) {
          try {
            const parsed = JSON.parse(raw) as { token: string; user: AuthUser };
            this.token = parsed.token;
            this.user = parsed.user;
          } catch {
            localStorage.removeItem("auth");
          }
        }
      }
    },
  },
});
