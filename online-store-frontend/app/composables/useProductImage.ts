// Product images live on the API host's wwwroot, not under /api.
export const useProductImage = () => {
  const apiBase = useApi();
  const origin = apiBase.replace(/\/api\/?$/, "");

  return (imageUrl: string | null | undefined): string | null => {
    if (!imageUrl) return null;
    if (/^https?:\/\//i.test(imageUrl)) return imageUrl;
    return `${origin}${imageUrl.startsWith("/") ? "" : "/"}${imageUrl}`;
  };
};
