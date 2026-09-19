import { Link, createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  component: Home,
});

function Home() {
  return (
    <main>
      <h1>MCA</h1>
      <p>TanStack Start surface. Same OpenAPI + OIDC PKCE client as Astro.</p>
      <p>
        <Link to="/login">Sign in</Link>
      </p>
    </main>
  );
}
