export const apiUrl = (
  import.meta.env.VITE_API_URL as string | undefined
)?.replace(/\/+$/, "") || "http://localhost:__HTTP_PORT__";

export const spaOrigin =
  typeof window === "undefined" ? "http://localhost:3000" : window.location.origin;
