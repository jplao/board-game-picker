import type { BoardGame, GameFilter } from "./types";

const BASE = "http://localhost:5000/api";

function buildQuery(filter: GameFilter): string {
  const params = new URLSearchParams();
  if (filter.playerCount) params.set("playerCount", String(filter.playerCount));
  if (filter.maxAge) params.set("maxAge", String(filter.maxAge));
  if (filter.type) params.set("type", filter.type);
  if (filter.category) params.set("category", filter.category);
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
