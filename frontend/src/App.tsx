import { useEffect, useRef, useState } from "react";
import { fetchTypes, fetchCategories, fetchRandomGame, fetchGames, createGame } from "./api";
import type { BoardGame, GameFilter } from "./types";
import "./App.css";

const PLAYER_OPTIONS = [1, 2, 3, 4, 5, 6, 7, 8];
const AGE_OPTIONS = [6, 8, 10, 12, 13, 14, 15, 18];
const RUNTIME_OPTIONS = [30, 45, 60, 90, 120, 180, 240, 480];

type Tab = "pick" | "collection" | "add";

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

  // Pick tab
  const [filter, setFilter] = useState<GameFilter>({});
  const [randomGame, setRandomGame] = useState<BoardGame | null>(null);
  const [matchingGames, setMatchingGames] = useState<BoardGame[]>([]);
  const [pickError, setPickError] = useState<string | null>(null);
  const [pickLoading, setPickLoading] = useState(false);
  const [showList, setShowList] = useState(false);

  // Collection tab
  const [collection, setCollection] = useState<BoardGame[]>([]);
  const [collectionLoading, setCollectionLoading] = useState(false);
  const [collectionError, setCollectionError] = useState<string | null>(null);

  // Add tab
  const [form, setForm] = useState(EMPTY_FORM);
  const [addError, setAddError] = useState<string | null>(null);
  const [addSuccess, setAddSuccess] = useState(false);
  const [addLoading, setAddLoading] = useState(false);

  const refreshMeta = () => {
    fetchTypes().then(setTypes).catch(console.error);
    fetchCategories().then(setCategories).catch(console.error);
  };

  useEffect(refreshMeta, []);

  useEffect(() => {
    if (tab !== "collection") return;
    setCollectionLoading(true);
    setCollectionError(null);
    fetchGames({})
      .then(setCollection)
      .catch(() => setCollectionError("Failed to load collection"))
      .finally(() => setCollectionLoading(false));
  }, [tab]);

  async function handlePickGame() {
    setPickLoading(true);
    setPickError(null);
    setRandomGame(null);
    try {
      setRandomGame(await fetchRandomGame(filter));
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
      setMatchingGames(await fetchGames(filter));
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
      refreshMeta();
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
        <button className={tab === "collection" ? "tab active" : "tab"} onClick={() => setTab("collection")}>
          📚 My Collection
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
              <div className="filter-item">
                <span className="filter-label">Players</span>
                <select value={filter.playerCount ?? ""} onChange={(e) => setFilter({ ...filter, playerCount: e.target.value ? +e.target.value : undefined })}>
                  <option value="">Any</option>
                  {PLAYER_OPTIONS.map((n) => <option key={n} value={n}>{n}</option>)}
                </select>
              </div>
              <div className="filter-item">
                <span className="filter-label">Min Age</span>
                <select value={filter.maxAge ?? ""} onChange={(e) => setFilter({ ...filter, maxAge: e.target.value ? +e.target.value : undefined })}>
                  <option value="">Any</option>
                  {AGE_OPTIONS.map((n) => <option key={n} value={n}>{n}+</option>)}
                </select>
              </div>
              <div className="filter-item">
                <span className="filter-label">Max Runtime</span>
                <select value={filter.maxRuntime ?? ""} onChange={(e) => setFilter({ ...filter, maxRuntime: e.target.value ? +e.target.value : undefined })}>
                  <option value="">Any</option>
                  {RUNTIME_OPTIONS.map((n) => <option key={n} value={n}>{n} min</option>)}
                </select>
              </div>
              <div className="filter-item">
                <span className="filter-label">Type</span>
                <select value={filter.type ?? ""} onChange={(e) => setFilter({ ...filter, type: e.target.value || undefined })}>
                  <option value="">Any</option>
                  {types.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div className="filter-item">
                <span className="filter-label">
                  Category
                  {(filter.categories?.length ?? 0) > 0 && (
                    <span className="filter-count">{filter.categories!.length}</span>
                  )}
                </span>
                <CategoryMultiSelect
                  options={categories}
                  selected={filter.categories ?? []}
                  onChange={(cats) => setFilter({ ...filter, categories: cats.length ? cats : undefined })}
                />
              </div>
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

      {tab === "collection" && (
        <section className="card collection">
          <div className="collection-header">
            <h2>My Collection</h2>
            {!collectionLoading && !collectionError && (
              <span className="collection-count">{collection.length} game{collection.length !== 1 ? "s" : ""}</span>
            )}
          </div>
          {collectionLoading && <p className="collection-status">Loading…</p>}
          {collectionError && <div className="banner error">{collectionError}</div>}
          {!collectionLoading && !collectionError && collection.length === 0 && (
            <p className="collection-status">No games yet — add some from the ➕ tab!</p>
          )}
          <div className="game-grid">
            {collection.map((g) => <GameCard key={g.id} game={g} />)}
          </div>
        </section>
      )}

      {tab === "add" && (
        <section className="card add-form">
          <h2>Add a Game to Your Collection</h2>
          {addSuccess && <div className="banner success">🎉 Game added to your collection!</div>}
          {addError && <div className="banner error">{addError}</div>}

          <form onSubmit={handleAddGame}>
            <div className="form-grid">
              <label className="span-2">
                Game Name *
                <input type="text" required value={form.name} onChange={(e) => field("name", e.target.value)} placeholder="e.g. Catan" />
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
                BGG Rank <span className="hint">(0 if unranked)</span>
                <input type="number" min={0} value={form.bggRank} onChange={(e) => field("bggRank", +e.target.value)} />
              </label>

              <div className="span-2 field-group">
                <span className="field-label">Type *</span>
                <PillSelect
                  options={types}
                  value={form.type}
                  onChange={(v) => field("type", v)}
                  name="type"
                  otherPlaceholder="Enter a new type…"
                />
              </div>

              <div className="span-2 field-group">
                <span className="field-label">Category *</span>
                <PillSelect
                  options={categories}
                  value={form.category}
                  onChange={(v) => field("category", v)}
                  name="category"
                  otherPlaceholder="Enter a new category…"
                />
              </div>

              <label className="span-2">
                Image URL <span className="hint">(optional)</span>
                <input type="url" value={form.imageUrl} onChange={(e) => field("imageUrl", e.target.value)} placeholder="https://…" />
              </label>

              <label className="span-2">
                Description *
                <textarea required rows={4} value={form.description} onChange={(e) => field("description", e.target.value)} placeholder="A short description of the game…" />
              </label>

              <label className="span-2 checkbox-label">
                <input type="checkbox" checked={form.isOwned} onChange={(e) => field("isOwned", e.target.checked)} />
                I own this game (uncheck to save for your wishlist)
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

// Pill-style radio select with an "Other" option
function PillSelect({
  options,
  value,
  onChange,
  otherPlaceholder = "Enter value…",
}: {
  options: string[];
  value: string;
  onChange: (v: string) => void;
  name?: string;
  otherPlaceholder?: string;
}) {
  const [showOther, setShowOther] = useState(!options.includes(value) && value !== "");

  function pick(opt: string) {
    setShowOther(false);
    onChange(opt);
  }

  function pickOther() {
    setShowOther(true);
    onChange("");
  }

  return (
    <div className="pill-select">
      {options.map((opt) => (
        <button
          key={opt}
          type="button"
          className={`pill ${value === opt && !showOther ? "selected" : ""}`}
          onClick={() => pick(opt)}
        >
          {opt}
        </button>
      ))}
      <button
        type="button"
        className={`pill pill-other ${showOther ? "selected" : ""}`}
        onClick={pickOther}
      >
        + Other
      </button>
      {showOther && (
        <input
          type="text"
          className="pill-other-input"
          placeholder={otherPlaceholder}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          autoFocus
        />
      )}
    </div>
  );
}

function CategoryMultiSelect({
  options,
  selected,
  onChange,
}: {
  options: string[];
  selected: string[];
  onChange: (cats: string[]) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  function toggle(cat: string) {
    onChange(selected.includes(cat) ? selected.filter((c) => c !== cat) : [...selected, cat]);
  }

  const label = selected.length === 0 ? "Any" : selected.length === 1 ? selected[0] : `${selected.length} selected`;

  return (
    <div className="multi-select" ref={ref}>
      <button type="button" className={`multi-select-trigger ${open ? "open" : ""}`} onClick={() => setOpen(!open)}>
        <span>{label}</span>
        <span className="caret">▾</span>
      </button>
      {open && (
        <div className="multi-select-dropdown">
          {options.map((cat) => (
            <label key={cat} className="multi-select-option">
              <input type="checkbox" checked={selected.includes(cat)} onChange={() => toggle(cat)} />
              {cat}
            </label>
          ))}
          {selected.length > 0 && (
            <button type="button" className="multi-select-clear" onClick={() => onChange([])}>
              Clear selection
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function GameCard({ game, featured = false }: { game: BoardGame; featured?: boolean }) {
  return (
    <div className={`game-card ${featured ? "featured" : ""}`}>
      {game.imageUrl && (
        <img src={game.imageUrl} alt={game.name} onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }} />
      )}
      <div className="game-info">
        {game.bggRank > 0 && <div className="game-rank">BGG #{game.bggRank}</div>}
        <h3>{game.name}</h3>
        <p className="description">{game.description}</p>
        <div className="game-meta">
          <span>👥 {game.minPlayers === game.maxPlayers ? game.minPlayers : `${game.minPlayers}–${game.maxPlayers}`}</span>
          <span>⏱ {game.minRuntime === game.maxRuntime ? game.minRuntime : `${game.minRuntime}–${game.maxRuntime}`} min</span>
          <span>🎂 {game.minAge}+</span>
          <span className="tag type-tag">{game.type}</span>
          <span className="tag cat-tag">{game.category}</span>
        </div>
      </div>
    </div>
  );
}
