import { useState } from "react";
import { Button, SafeAreaView, Text, TextInput, View } from "react-native";
import * as SecureStore from "expo-secure-store";

const apiUrl = process.env.EXPO_PUBLIC_API_URL || "http://localhost:__HTTP_PORT__";

export default function App() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [status, setStatus] = useState("Sign in against the generated --auth API.");
  const [todos, setTodos] = useState([]);

  async function login() {
    try {
      const body = new URLSearchParams({
        grant_type: "password",
        username: email,
        password,
        client_id: "mca-web-client",
        client_secret: "mca-default-secret-change-me",
        scope: "openid profile email offline_access mca.api",
      });
      const res = await fetch(`${apiUrl}/connect/token`, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: body.toString(),
      });
      if (!res.ok) {
        setStatus(`Login failed (${res.status})`);
        return;
      }
      const tokens = await res.json();
      await SecureStore.setItemAsync("access_token", tokens.access_token);
      setStatus("Signed in.");
      await loadTodos(tokens.access_token);
    } catch (err) {
      setStatus(String(err));
    }
  }

  async function loadTodos(token) {
    const res = await fetch(`${apiUrl}/api/todos`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      setStatus(`Todos failed (${res.status})`);
      return;
    }
    setTodos(await res.json());
  }

  return (
    <SafeAreaView style={{ flex: 1, padding: 16, gap: 8 }}>
      <Text>{status}</Text>
      <TextInput placeholder="email" autoCapitalize="none" value={email} onChangeText={setEmail} />
      <TextInput placeholder="password" secureTextEntry value={password} onChangeText={setPassword} />
      <Button title="Sign in" onPress={() => void login()} />
      <View>
        {todos.map((t) => (
          <Text key={t.id}>{t.title}</Text>
        ))}
      </View>
    </SafeAreaView>
  );
}
