# MCA mobile (Expo)

Official Expo scaffold. Emitted by `--mobile`. Pair with `--auth`.

```bash
dotnet run --project src/MCA.Api
cd apps/mobile && npx expo start
```

Sign in uses the resource-owner password grant against `/connect/token` (`mca-web-client`) and stores the access token in `expo-secure-store`. Todos load with `Authorization: Bearer`. Set `EXPO_PUBLIC_API_URL` if the API is not `http://localhost:__HTTP_PORT__`.

This folder is not a .NET project. Do not import `MinimalCleanArch.Extensions`.
