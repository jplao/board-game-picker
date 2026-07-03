import { useEffect, useState } from "react";
import { fetchTypes, fetchCategories, fetchRandomGame, fetchGames, createGame } from "./api";
import type { BoardGame, GameFilter } from "./types";
import "./App.css";

const PLAYER_OPTIONS = [1, 2, 3, 4, 5, 6, 7, 8];
const AGE_OPTIONS = [6, 8, 10, 12, 13, 14, 15, 18];
const RUNTIME_OPTIONS = [30, 45, 60, 90, 120, 180, 240, 480];

type Tab = "pick" | "add";

const EMPTY_FORM = {
  name: "",
  minPlayers: 2,
  maxPlayers: 4,
  minRuntime: 30,
  maxRuntime: 60,
  minAge: 10,
  imageUrl: "",
  description: "",
  type: "",
  category: "",
  bggRank: 0,
  isOwned: true,
};

export default function App() {
  const [tab, setTab] = useState<Tab>("pick");
  const [types, setTypes] = useState<string[]>([]);
  const [categories, setCategories] = useState<string[]>([]);

  // Pick tab state
  const [filter, setFilter] = useState<GameFilter>({});
  const [randomGame, setRandomGame] = useState<BoardGame | null>(null);
  const [matchingGames, setMatchingGames] = useState<BoardGame[]>([]);
  const [pickError, setPickError] = useState<string | null>(null);
  const [pickLoading, setPickLoading] = useState(false);
  const [showList, setShowList] = useState(false);

  // Add tab state
  const [form, setForm] = useState(EMPTY_FORM);
  const [addError, setAddError] = useState<string | null>(null);
  const [addSuccess, setAddSuccess] = useState(false);
  const [addLoading, setAddLoading] = useState(false);

  useEffect(() => {
    fetchTypes().then(setTypes).catch(console.error);
    fetchCategories().then(setCategories).catch(console.error);
  }, []);

  async function handlePickGame() {
    setPickLoading(true);
    setPickError(null);
    setRandomGame(null);
    try {
      const game = await fetchRandomGame(filter);
      setRandomGame(game);
    } catch (e) {
      setPickError(e instanceof Error ? e.message : "Something went wrong");
    } finally {
      setPickLoading(false);
    }
  }

  async function handleShowMatches() {
    setPickLoading(true);
    setPickError(null);
    try {
      const games = await fetchGames(filter);
      setMatchingGames(games);
      setShowList(true);
    } catch (e) {
      setPickError(e instanceof Error ? e.message : "Something went wrong");
    } finally {
      setPickLoading(false);
    }
  }

  function resetFilter() {
    setFilter({});
    setRandomGame(null);
    setPickError(null);
    setShowList(false);
    setMatchingGames([]);
  }

  async function handleAddGame(e: React.FormEvent) {
    e.preventDefault();
    setAddLoading(true);
    setAddError(null);
    setAddSuccess(false);
    try {
      await createGame({ ...form, imageUrl: form.imageUrl || null });
      setAddSuccess(true);
      setForm(EMPTY_FORM);
      fetchTypes().then(setTypes).catch(console.error);
      fetchCategories().then(setCategories).catch(console.error);
    } catch (err) {
      setAddError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setAddLoading(false);
    }
  }

  function field(key: keyof typeof EMPTY_FORM, value: string | number | boolean) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <div className="app">
      <header>
        <h1>🎲 Board Game Picker</h1>
        <p>Your collection, one roll of the dice away</p>
      </header>

      <nav className="tabs">
        <button className={tab === "pick" ? "tab active" : "tab"} onClick={() => setTab("pick")}>
          🎲 Pick a Game
        </button>
        <button className={tab === "add" ? "tab active" : "tab"} onClick={() => setTab("add")}>
          ➕ Add a Game
        </button>
      </nav>

      {tab === "pick" && (
        <>
          <section className="card filters">
            <h2>Filter Your Collection</h2>
            <div className="filter-grid">
              <label>
                Players
                <select
                  value={filter.playerCount ?? ""}
                  onChange={(e) => setFilter({ ...filter, playerCount: e.target.value ? +e.target.value : undefined })}
                >
                  <option value="">Any</option>
                  {PLAYER_OPTIONS.map((n) => <option key={n} value={n}>{n}</option>)}
                </select>
              </label>
              <label>
                Youngest Player Age
                <select
                  value={filter.maxAge ?? ""}
                  onChange={(e) => setFilter({ ...filter, maxAge: e.target.value ? +e.target.value : undefined })}
                >
                  <option value="">Any</option>
                  {AGE_OPTIONS.map((n) => <option key={n} value={n}>{n}+</option>)}
                </select>
              </label>
              <label>
                Max Runtime
                <select
                  value={filter.maxRuntime ?? ""}
                  onChange={(e) => setFilter({ ...filter, maxRuntime: e.target.value ? +e.target.value : undefined })}
                >
                  <option value="">Any</option>
                  {RUNTIME_OPTIONS.map((n) => <option key={n} value={n}>{n} min</option>)}
                </select>
              </label>
              <label>
                Type
                <select
                  value={filter.type ?? ""}
                  onChange={(e) => setFilter({ ...filter, type: e.target.value || undefined })}
                >
                  <option value="">Any</option>
                  {types.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </label>
              <label>
                Category
                <select
                  value={filter.category ?? ""}
                  onChange={(e) => setFilter({ ...filter, category: e.target.value || undefined })}
                >
                  <option value="">Any</option>
                  {categories.map((c) => <option key={c} value={c}>{c}</option>)}
                </select>
              </label>
            </div>
            <div className="filter-actions">
              <button className="btn-primary" onClick={handlePickGame} disabled={pickLoading}>
                {pickLoading ? "Picking…" : "🎲 Pick Random Game"}
              </button>
              <button className="btn-secondary" onClick={handleShowMatches} disabled={pickLoading}>
                {pickLoading ? "Loading…" : "📋 Show All Matches"}
              </button>
              <button className="btn-ghost" onClick={resetFilter}>Reset</button>
            </div>
          </section>

          {pickError && <div className="banner error">{pickError}</div>}

          {randomGame && (
            <section className="result">
              <h2>Tonight you're playing…</h2>
              <GameCard game={randomGame} featured />
              <button className="btn-secondary" onClick={handlePickGame} disabled={pickLoading}>
                🔀 Pick Again
              </button>
            </section>
          )}

          {showList && (
            <section className="game-list">
              <div className="list-header">
                <h2>{matchingGames.length} game{matchingGames.length !== 1 ? "s" : ""} match your filters</h2>
                <button className="btn-ghost" onClick={() => setShowList(false)}>Close</button>
              </div>
              <div className="game-grid">
                {matchingGames.map((g) => <GameCard key={g.id} game={g} />)}
              </div>
            </section>
          )}
        </>
      )}

      {tab === "add" && (
        <section className="card add-form">
          <h2>Add a Game to Your Collection</h2>
          {addSuccess && (
            <div className="banner success">Game added successfully!</div>
          )}
          {addError && <div className="banner error">{addError}</div>}

          <form onSubmit={handleAddGame}>
            <div className="form-grid">
              <label className="span-2">
                Game Name *
                <input required value={form.name} onChange={(e) => field("name", e.target.value)} placeholder="e.g. Catan" />
              </label>

              <label>
                Min Players *
                <input type="number" required min={1} max={99} value={form.minPlayers} onChange={(e) => field("minPlayers", +e.target.value)} />
              </label>
              <label>
                Max Players *
                <input type="number" required min={1} max={99} value={form.maxPlayers} onChange={(e) => field("maxPlayers", +e.target.value)} />
              </label>

              <label>
                Min Runtime (min) *
                <input type="number" required min={1} value={form.minRuntime} onChange={(e) => field("minRuntime", +e.target.value)} />
              </label>
              <label>
                Max Runtime (min) *
                <input type="number" required min={1} value={form.maxRuntime} onChange={(e) => field("maxRuntime", +e.target.value)} />
              </label>

              <label>
                Min Age *
                <input type="number" required min={1} max={99} value={form.minAge} onChange={(e) => field("minAge", +e.target.value)} />
              </label>
              <label>
                BGG Rank
                <input type="number" min={0} value={form.bggRank} onChange={(e) => field("bggRank", +e.target.value)} placeholder="0 if unranked" />
              </label>

              <label>
                Type *
                <input required value={form.type} onChange={(e) => field("type", e.target.value)} placeholder="e.g. Strategy, Party, Co-op" list="types-list" />
                <datalist id="types-list">{types.map((t) => <option key={t} value={t} />)}</datalist>
              </label>
              <label>
                Category *
                <input required value={form.category} onChange={(e) => field("category", e.target.value)} placeholder="e.g. Deckbuilding, Worker Placement" list="categories-list" />
                <datalist id="categories-list">{categories.map((c) => <option key={c} value={c} />)}</datalist>
              </label>

              <label className="span-2">
                Image URL
                <input type="url" value={form.imageUrl} onChange={(e) => field("imageUrl", e.target.value)} placeholder="https://…" />
              </label>

              <label className="span-2">
                Description *
                <textarea required rows={3} value={form.description} onChange={(e) => field("description", e.target.value)} placeholder="A short description of the game…" />
              </label>

              <label className="span-2 checkbox-label">
                <input type="checkbox" checked={form.isOwned} onChange={(e) => field("isOwned", e.target.checked)} />
                I own this game
              </label>
            </div>

            <div className="form-actions">
              <button type="submit" className="btn-primary" disabled={addLoading}>
                {addLoading ? "Adding…" : "➕ Add to Collection"}
              </button>
              <button type="button" className="btn-ghost" onClick={() => setForm(EMPTY_FORM)}>
                Clear
              </button>
            </div>
          </form>
        </section>
      )}
    </div>
  );
}

function GameCard({ game, featured = false }: { game: BoardGame; featured?: boolean }) {
  return (
    <div className={`game-card ${featured ? "featured" : ""}`}>
      {game.imageUrl && (
        <img
          src={game.imageUrl}
          alt={game.name}
          onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }}
        />
      )}
      <div className="game-info">
        {game.bggRank > 0 && <div className="game-rank">BGG #{game.bggRank}</div>}
        <h3>{game.name}</h3>
        <p className="description">{game.description}</p>
        <div className="game-meta">
          <span>👥 {game.minPlayers === game.maxPlayers ? game.minPlayers : `${game.minPlayers}–${game.maxPlayers}`}</span>
          <span>⏱ {game.minRuntime === game.maxRuntime ? game.minRuntime : `${game.minRuntime}–${game.maxRuntime}`} min</span>
          <span>🎂 {game.minAge}+</span>
          <span className="tag">{game.type}</span>
          <span className="tag">{game.category}</span>
        </div>
      </div>
    </div>
  );
}
