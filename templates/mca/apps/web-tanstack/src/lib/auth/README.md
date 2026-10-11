# OIDC PKCE client

Framework-neutral TypeScript client for the generated `--auth` API (OpenIddict).

- authorization-code + PKCE (`/connect/authorize` → `/connect/token`)
- refresh (`grant_type=refresh_token` via silent renew)
- `fetch` attaches `Authorization: Bearer`

Public client id: `mca-spa-client` (no secret). First-party origins listed on the API:

- `http://localhost:4321/callback` (Astro default)
- `http://localhost:3000/callback` (TanStack / Vite default)

```ts
import { createMcaAuth } from "./lib/auth";

const auth = createMcaAuth({
  authority: "http://localhost:__HTTP_PORT__",
  redirectUri: `${window.location.origin}/callback`,
});

await auth.login();
// on /callback:
await auth.handleCallback();
const todos = await auth.fetch("http://localhost:__HTTP_PORT__/api/todos");
```

Do not import `MinimalCleanArch.Extensions` from this folder. The contract is OpenAPI + OIDC only.

Install: `npm install` in `apps/web` (dependency `oidc-client-ts`).
