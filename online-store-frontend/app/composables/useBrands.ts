export interface BrandOption {
  id: number;
  name: string;
}

// Client-only: the .NET dev server's self-signed HTTPS cert is rejected by
// Nitro's SSR fetch (same reason as useProducts.ts).
export const useBrands = () => {
  const apiBase = useApi();
  return useFetch<BrandOption[]>(`${apiBase}/brands`, {
    key: "brands-list",
    server: false,
    default: (): BrandOption[] => [],
  });
};
