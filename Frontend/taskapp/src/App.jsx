import { useState, useEffect } from "react";
import { login, me, getTodos, addTodo, updateTodo, deleteTodo } from "./api";
import "./styles.css";

export default function App() {
  const [username, setUsername] = useState("matt");
  const [password, setPassword] = useState("secret123");
  const [user, setUser] = useState(null);
  const [todos, setTodos] = useState([]);
  const [newTitle, setNewTitle] = useState("");
  const [darkMode, setDarkMode] = useState(false);

  //dark/light mode previous stored
  useEffect(() => {
    const saved = localStorage.getItem("darkMode") === "true";
    setDarkMode(saved);
    if (saved) {
      document.body.classList.add("dark");
    }
  }, []);

  function toggleDarkMode() {
    setDarkMode((prev) => {
      const newMode = !prev;
      localStorage.setItem("darkMode", newMode);
      if (newMode) {
        document.body.classList.add("dark");
      } else {
        document.body.classList.remove("dark");
      }
      return newMode;
    });
  }

  async function handleLogin() {
    await login(username, password);
    const userRes = await me();
    setUser(userRes.data);
    loadTodos();
  }

  function handleLogout() {
    localStorage.removeItem("token");
    setUser(null);
    setTodos([]);
  }

  async function loadTodos() {
    const res = await getTodos();
    setTodos(res.data);
  }

  async function handleAdd() {
    if (!newTitle) return;
    await addTodo(newTitle);
    setNewTitle("");
    loadTodos();
  }

  async function handleToggle(todo) {
    await updateTodo(todo.id, { title: todo.title, done: !todo.done });
    loadTodos();
  }

  async function handleDelete(id) {
  const el = document.getElementById(`todo-${id}`);
  if (el) {
    el.classList.add("fade-out");
    setTimeout(async () => {
      await deleteTodo(id);
      loadTodos();
    }, 300);
  }
}

  return (
  <div className={user ? "top-center" : "center-screen"}>
    <div className="card">
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>Task Manager</h1>
        <button onClick={toggleDarkMode} className="btn-gray">
          {darkMode ? "Light" : "Dark"}
        </button>
      </div>

      {!user ? (
        <div style={{ marginTop: "1.5rem" }}>
          <h2>Login</h2>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="Username"
            style={{ display: "block", marginBottom: "0.5rem", width: "100%" }}
          />
          <input
            value={password}
            type="password"
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Password"
            style={{ display: "block", marginBottom: "0.5rem", width: "100%" }}
          />
          <button onClick={handleLogin} className="btn-blue" style={{ width: "100%" }}>
            Login
          </button>
        </div>
      ) : (
        <div style={{ marginTop: "1.5rem" }}>
          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "1rem" }}>
            <p>
              Welcome, <b>{user.username}</b>
            </p>
            <button onClick={handleLogout} className="btn-gray">
              Logout
            </button>
          </div>

          <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem" }}>
            <input
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="New todo"
            />
            <button onClick={handleAdd} className="btn-green">
              Add
            </button>
          </div>

          <h2>Your Todos</h2>
          {todos.length === 0 ? (
            <p style={{ color: "gray", textAlign: "center", marginTop: "1rem" }}>
              Nothing here yet — add a task above!
            </p>
          ) : (
            <ul style={{ marginTop: "1rem", padding: 0, listStyle: "none" }}>
              {todos.map((t) => (
                <li key={t.id} id={`todo-${t.id}`} className="todo-item">
                  <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                    <input
                      type="checkbox"
                      checked={t.done}
                      onChange={() => handleToggle(t)}
                    />
                    <span className={`todo-title ${t.done ? "done" : ""}`}>
                      {t.title}
                    </span>
                  </div>
                  <button onClick={() => handleDelete(t.id)} className="btn-red">
                    Delete
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  </div>
);
}