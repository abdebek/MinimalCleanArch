import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { getAuth } from "../lib/auth-instance";

export const Route = createFileRoute("/callback")({
  component: CallbackPage,
});

function CallbackPage() {
  const navigate = useNavigate();
  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        await getAuth().handleCallback();
      } catch {
        // Already redeemed on a Strict Mode remount, or a real failure.
      }
      if (cancelled) return;
      const user = await getAuth().getUser();
      await navigate({ to: user ? "/todos" : "/login" });
    })();
    return () => {
      cancelled = true;
    };
  }, [navigate]);
  return <p>Finishing sign-in…</p>;
}
