export const useApi = () => {
  const config = useRuntimeConfig();
  return config.public.apiBase;
};
