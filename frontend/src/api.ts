import type { AuthUser, BoardGame, GameFilter } from "./types";

const BASE = `${import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000"}/api`;

function authHeaders(token: string) {
  return { "Content-Type": "application/json", Authorization: `Bearer ${token}` };
}

function buildQuery(filter: GameFilter): string {
  const params = new URLSearchParams();
  if (filter.playerCount) params.set("playerCount", String(filter.playerCount));
  if (filter.maxAge) params.set("maxAge", String(filter.maxAge));
  if (filter.type) params.set("type", filter.type);
  filter.categories?.forEach((c) => params.append("categories", c));
  if (filter.maxRuntime) params.set("maxRuntime", String(filter.maxRuntime));
  return params.toString();
}

export async function login(email: string, password: string): Promise<AuthUser> {
  const res = await fetch(`${BASE}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error("Invalid email or password");
  const data = await res.json();
  return { token: data.token, email: data.email, role: data.role };
}

export async function register(email: string, password: string): Promise<AuthUser> {
  const res = await fetch(`${BASE}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (res.status === 409) throw new Error("An account with that email already exists");
  if (!res.ok) throw new Error("Registration failed");
  const data = await res.json();
  return { token: data.token, email: data.email, role: data.role };
}

export async function fetchGames(token: string, filter: GameFilter): Promise<BoardGame[]> {
  const res = await fetch(`${BASE}/games?${buildQuery(filter)}`, {
    headers: authHeaders(token),
  });
  if (!res.ok) throw new Error("Failed to fetch games");
  return res.json();
}

export async function fetchRandomGame(token: string, filter: GameFilter): Promise<BoardGame> {
  const res = await fetch(`${BASE}/games/random?${buildQuery(filter)}`, {
    headers: authHeaders(token),
  });
  if (res.status === 404) throw new Error("No games match your criteria");
  if (!res.ok) throw new Error("Failed to fetch random game");
  return res.json();
}

export async function fetchTypes(token: string): Promise<string[]> {
  const res = await fetch(`${BASE}/games/types`, { headers: authHeaders(token) });
  return res.json();
}

export async function fetchCategories(token: string): Promise<string[]> {
  const res = await fetch(`${BASE}/games/categories`, { headers: authHeaders(token) });
  return res.json();
}

export async function createGame(token: string, game: Omit<BoardGame, "id">): Promise<BoardGame> {
  const res = await fetch(`${BASE}/games`, {
    method: "POST",
    headers: authHeaders(token),
    body: JSON.stringify(game),
  });
  if (!res.ok) throw new Error("Failed to add game");
  return res.json();
}
