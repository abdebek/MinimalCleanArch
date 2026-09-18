import { auth } from "../lib/auth-instance";

export function LoginPage() {
  return (
    <main>
      <h1>Sign in</h1>
      <p>Vite + React surface. Same OIDC PKCE client as Astro (`src/lib/auth`).</p>
      <button type="button" onClick={() => void auth.login()}>
        Continue with OpenID Connect
      </button>
    </main>
  );
}
