export interface CategoryOption {
  id: number;
  name: string;
}

// Client-only: the .NET dev server's self-signed HTTPS cert is rejected by
// Nitro's SSR fetch (same reason as useProducts.ts).
export const useCategories = () => {
  const apiBase = useApi();
  return useFetch<CategoryOption[]>(`${apiBase}/categories`, {
    key: "categories-list",
    server: false,
    default: (): CategoryOption[] => [],
  });
};
