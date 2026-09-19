import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { auth } from "../lib/auth-instance";

export const Route = createFileRoute("/callback")({
  component: CallbackPage,
});

function CallbackPage() {
  const navigate = useNavigate();
  useEffect(() => {
    void auth.handleCallback().then(
      () => navigate({ to: "/todos" }),
      () => navigate({ to: "/login" }),
    );
  }, [navigate]);
  return <p>Finishing sign-in…</p>;
}
