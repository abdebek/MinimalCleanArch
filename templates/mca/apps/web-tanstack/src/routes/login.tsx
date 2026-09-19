import { createFileRoute } from "@tanstack/react-router";
import { auth } from "../lib/auth-instance";

export const Route = createFileRoute("/login")({
  component: LoginPage,
});

function LoginPage() {
  return (
    <main>
      <h1>Sign in</h1>
      <button type="button" onClick={() => void auth.login()}>
        Continue with OpenID Connect
      </button>
    </main>
  );
}
