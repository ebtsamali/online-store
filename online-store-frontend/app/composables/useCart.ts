export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  subtotal: number;
}

export interface CartResponse {
  items: CartItem[];
  total: number;
}

export const useCart = () => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<CartResponse>(`${apiBase}/cart`, {
    key: "cart",
    headers: { Authorization: `Bearer ${auth.token}` },
    // Same reason as useProducts/useProduct: the .NET dev server's
    // self-signed HTTPS cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};
