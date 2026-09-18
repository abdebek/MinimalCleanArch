# MCA web (Vite + React)

Second official web adapter. Same OpenAPI + OIDC PKCE contract as Astro (`src/lib/auth`). Emitted by `--frontend --frontend-framework tanstack`.

```bash
dotnet run --project src/MCA.Api
cd apps/web && npm install && npm run dev
```

Open http://localhost:3000 (listed on `mca-spa-client`).
