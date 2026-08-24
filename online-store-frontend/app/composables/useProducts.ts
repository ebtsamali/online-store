export interface ProductListItem {
  id: number;
  name: string;
  price: number;
  stock: number;
  isActive: boolean;
  imageUrl: string;
}

export interface PagedProducts {
  items: ProductListItem[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProductDetail {
  id: number;
  name: string;
  description: string;
  price: number;
  stock: number;
  categoryId: number;
  brandId: number;
  isActive: boolean;
  createdAt: string;
  imageUrl: string;
}

// Pass a distinct `key` per call site so separate pages don't share one cached
// payload.
export const useProducts = (
  page: number,
  pageSize: number,
  key: string,
  search?: string,
) => {
  const apiBase = useApi();
  return useFetch<PagedProducts>(`${apiBase}/products`, {
    key,
    query: { page, pageSize, search: search || undefined },
    // Client-only: the .NET dev server uses a self-signed HTTPS certificate,
    // which the Nitro server rejects during SSR.
    server: false,
    default: (): PagedProducts => ({ items: [], page, pageSize, total: 0 }),
  });
};

export const useProduct = (id: number) => {
  const apiBase = useApi();
  return useFetch<ProductDetail>(`${apiBase}/products/${id}`, {
    key: `product-${id}`,
    // Same reason as useProducts: the .NET dev server's self-signed HTTPS
    // cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};
