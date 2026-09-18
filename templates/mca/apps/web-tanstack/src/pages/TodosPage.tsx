import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { auth } from "../lib/auth-instance";
import { apiUrl } from "../lib/config";

type Todo = { id: number; title: string; isCompleted: boolean };

export function TodosPage() {
  const navigate = useNavigate();
  const [todos, setTodos] = useState<Todo[]>([]);
  const [title, setTitle] = useState("");

  useEffect(() => {
    void (async () => {
      const user = await auth.getUser();
      if (!user) {
        navigate("/login", { replace: true });
        return;
      }
      const res = await auth.fetch(`${apiUrl}/api/todos`);
      if (res.ok) setTodos(await res.json());
    })();
  }, [navigate]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    const res = await auth.fetch(`${apiUrl}/api/todos`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title, priority: 0 }),
    });
    if (res.ok) {
      setTitle("");
      const list = await auth.fetch(`${apiUrl}/api/todos`);
      if (list.ok) setTodos(await list.json());
    }
  }

  return (
    <main>
      <h1>Todos</h1>
      <form onSubmit={(e) => void onSubmit(e)}>
        <input value={title} onChange={(ev) => setTitle(ev.target.value)} required />
        <button type="submit">Add</button>
      </form>
      <ul>
        {todos.map((t) => (
          <li key={t.id}>{t.title}</li>
        ))}
      </ul>
      <button type="button" onClick={() => void auth.logout()}>
        Sign out
      </button>
    </main>
  );
}
