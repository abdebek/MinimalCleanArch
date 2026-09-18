import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { auth } from "../lib/auth-instance";

export function CallbackPage() {
  const navigate = useNavigate();
  useEffect(() => {
    void auth.handleCallback().then(
      () => navigate("/todos", { replace: true }),
      () => navigate("/login", { replace: true }),
    );
  }, [navigate]);
  return <p>Finishing sign-in…</p>;
}
