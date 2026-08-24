export interface OrderSummary {
  id: number;
  status: string;
  total: number;
  createdAt: string;
}

export interface OrderItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
}

export interface OrderDetail {
  id: number;
  status: string;
  total: number;
  createdAt: string;
  items: OrderItem[];
}

export const useOrders = () => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<OrderSummary[]>(`${apiBase}/orders`, {
    key: "orders",
    headers: { Authorization: `Bearer ${auth.token}` },
    // Same reason as useCart/useProducts: the .NET dev server's self-signed
    // HTTPS cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};

export const useOrder = (id: number) => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<OrderDetail>(`${apiBase}/orders/${id}`, {
    key: `order-${id}`,
    headers: { Authorization: `Bearer ${auth.token}` },
    server: false,
  });
};
