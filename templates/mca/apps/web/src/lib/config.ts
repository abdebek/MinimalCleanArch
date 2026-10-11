/** Generated API origin. `__HTTP_PORT__` is replaced by `dotnet new mca`. */
export const apiUrl = (
  import.meta.env.PUBLIC_API_URL as string | undefined
)?.replace(/\/+$/, "") || "http://localhost:__HTTP_PORT__";

export const spaOrigin =
  typeof window === "undefined" ? "http://localhost:4321" : window.location.origin;
