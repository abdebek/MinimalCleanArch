import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { FormEvent, useEffect, useState } from "react";
import { getAuth } from "../lib/auth-instance";
import { apiUrl } from "../lib/config";

export const Route = createFileRoute("/todos")({
  component: TodosPage,
});

type Todo = { id: number; title: string; isCompleted: boolean };

function TodosPage() {
  const navigate = useNavigate();
  const [todos, setTodos] = useState<Todo[]>([]);
  const [title, setTitle] = useState("");

  useEffect(() => {
    void (async () => {
      const user = await getAuth().getUser();
      if (!user) {
        await navigate({ to: "/login" });
        return;
      }
      const res = await getAuth().fetch(`${apiUrl}/api/todos`);
      if (res.ok) setTodos(await res.json());
    })();
  }, [navigate]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    const res = await getAuth().fetch(`${apiUrl}/api/todos`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title, priority: 0 }),
    });
    if (res.ok) {
      setTitle("");
      const list = await getAuth().fetch(`${apiUrl}/api/todos`);
      if (list.ok) setTodos(await list.json());
    }
  }

  return (
    <main>
      <h1>Todos</h1>
      <form onSubmit={(e) => void onSubmit(e)}>
        <input
          data-testid="todo-title"
          value={title}
          onChange={(ev) => setTitle(ev.target.value)}
          required
        />
        <button type="submit" data-testid="todo-add">
          Add
        </button>
      </form>
      <ul>
        {todos.map((t) => (
          <li key={t.id} data-testid="todo-item">
            {t.title}
          </li>
        ))}
      </ul>
      <button type="button" onClick={() => void getAuth().logout()}>
        Sign out
      </button>
    </main>
  );
}
