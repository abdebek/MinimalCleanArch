# MCA web (TanStack Start)

Second official web adapter. Same OpenAPI + OIDC PKCE contract as Astro (`src/lib/auth`). Emitted by `--frontend --webFramework tanstack`.

```bash
dotnet run --project src/MCA.Api
cd apps/web && npm install && npm run dev
```

Open http://localhost:3000 (listed on `mca-spa-client`). `src/routeTree.gen.ts` is created on first `npm run dev`.

Playwright smoke (API + `npm run dev` must be running):

```bash
API_URL=http://localhost:__HTTP_PORT__ WEB_URL=http://localhost:3000 npm run test:e2e
```
