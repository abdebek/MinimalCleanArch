# MCA web (Astro)

Official web surface. Emitted by `--frontend` (default framework: Astro). Pair with `--auth`.

```bash
dotnet run --project src/MCA.Api
cd apps/web && npm install && npm run dev
```

Open http://localhost:4321. The API stays at `http://localhost:__HTTP_PORT__` (`src/lib/config.ts`).

Pages: login (OIDC PKCE), signup, confirm-email, forgot/reset password, authenticated Todo list, logout, privacy/terms stubs. Locales: English and Arabic (RTL) via the header toggle (`src/lib/i18n.ts`).

`src/lib/auth` is the reusable client contract (`oidc-client-ts`). Do not import `MinimalCleanArch.Extensions` from this folder.

Playwright smoke: `API_URL=http://localhost:__HTTP_PORT__ npm run test:e2e` (API + `npm run dev` must be running).
