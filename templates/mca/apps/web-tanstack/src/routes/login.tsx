import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { getAuth } from "../lib/auth-instance";

export const Route = createFileRoute("/login")({
  component: LoginPage,
});

function LoginPage() {
  const [error, setError] = useState<string | null>(null);
  return (
    <main>
      <h1>Sign in</h1>
      <button
        type="button"
        data-testid="oidc-login"
        onClick={() => {
          void getAuth()
            .login()
            .catch((err: unknown) => setError(String(err)));
        }}
      >
        Continue with OpenID Connect
      </button>
      {error ? <p data-testid="auth-error">{error}</p> : null}
    </main>
  );
}
