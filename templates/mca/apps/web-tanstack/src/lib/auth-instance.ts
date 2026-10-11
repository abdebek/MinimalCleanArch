import { createMcaAuth, type McaAuthClient } from "./auth";
import { apiUrl } from "./config";

const windowKey = "__mcaAuth";

/** Browser-only. Lazy so SSR / TanStack Start does not pin origin to localhost:3000. */
export function getAuth(): McaAuthClient {
  if (typeof window === "undefined") {
    throw new Error("OIDC client is browser-only");
  }

  const w = window as Window & { [windowKey]?: McaAuthClient };
  if (!w[windowKey]) {
    const origin = window.location.origin;
    w[windowKey] = createMcaAuth({
      authority: apiUrl,
      redirectUri: `${origin}/callback`,
      postLogoutRedirectUri: `${origin}/`,
    });
  }

  return w[windowKey];
}

