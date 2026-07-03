import type { BoardGame, GameFilter } from "./types";

const BASE = `${import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000"}/api`;

function buildQuery(filter: GameFilter): string {
  const params = new URLSearchParams();
  if (filter.playerCount) params.set("playerCount", String(filter.playerCount));
  if (filter.maxAge) params.set("maxAge", String(filter.maxAge));
  if (filter.type) params.set("type", filter.type);
  filter.categories?.forEach((c) => params.append("categories", c));
  if (filter.maxRuntime) params.set("maxRuntime", String(filter.maxRuntime));
  return params.toString();
}

export async function fetchGames(filter: GameFilter): Promise<BoardGame[]> {
  const res = await fetch(`${BASE}/games?${buildQuery(filter)}`);
  if (!res.ok) throw new Error("Failed to fetch games");
  return res.json();
}

export async function fetchRandomGame(filter: GameFilter): Promise<BoardGame> {
  const res = await fetch(`${BASE}/games/random?${buildQuery(filter)}`);
  if (res.status === 404) throw new Error("No games match your criteria");
  if (!res.ok) throw new Error("Failed to fetch random game");
  return res.json();
}

export async function fetchTypes(): Promise<string[]> {
  const res = await fetch(`${BASE}/games/types`);
  return res.json();
}

export async function fetchCategories(): Promise<string[]> {
  const res = await fetch(`${BASE}/games/categories`);
  return res.json();
}

export async function createGame(game: Omit<BoardGame, "id">): Promise<BoardGame> {
  const res = await fetch(`${BASE}/games`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(game),
  });
  if (!res.ok) throw new Error("Failed to add game");
  return res.json();
}
