// The backend exposes `decimal` amounts with no currency field anywhere in the
// API (verified: no currency/ISO code in any DTO or entity), so the currency
// here is a frontend assumption. Change the ISO code in one place if the
// backend later returns one.
export function formatPrice(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}
