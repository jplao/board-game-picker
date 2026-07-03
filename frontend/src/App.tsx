import { useEffect, useState } from "react";
import { fetchTypes, fetchCategories, fetchRandomGame, fetchGames } from "./api";
import type { BoardGame, GameFilter } from "./types";
import "./App.css";

const PLAYER_OPTIONS = [1, 2, 3, 4, 5, 6, 7, 8];
const AGE_OPTIONS = [6, 8, 10, 12, 13, 14, 15, 18];
const RUNTIME_OPTIONS = [30, 45, 60, 90, 120, 180, 240, 480];

export default function App() {
  const [types, setTypes] = useState<string[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [filter, setFilter] = useState<GameFilter>({});
  const [randomGame, setRandomGame] = useState<BoardGame | null>(null);
  const [matchingGames, setMatchingGames] = useState<BoardGame[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showList, setShowList] = useState(false);

  useEffect(() => {
    fetchTypes().then(setTypes).catch(console.error);
    fetchCategories().then(setCategories).catch(console.error);
  }, []);

  async function handlePickGame() {
    setLoading(true);
    setError(null);
    setRandomGame(null);
    try {
      const game = await fetchRandomGame(filter);
      setRandomGame(game);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Something went wrong");
    } finally {
      setLoading(false);
    }
  }

  async function handleShowMatches() {
    setLoading(true);
    setError(null);
    try {
      const games = await fetchGames(filter);
      setMatchingGames(games);
      setShowList(true);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Something went wrong");
    } finally {
      setLoading(false);
    }
  }

  function resetFilter() {
    setFilter({});
    setRandomGame(null);
    setError(null);
    setShowList(false);
    setMatchingGames([]);
  }

  return (
    <div className="app">
      <header>
        <h1>🎲 Board Game Picker</h1>
        <p>Filter your collection and let fate decide what to play tonight</p>
      </header>

      <section className="filters">
        <h2>Filter Games</h2>
        <div className="filter-grid">
          <label>
            Players
            <select
              value={filter.playerCount ?? ""}
              onChange={(e) =>
                setFilter({ ...filter, playerCount: e.target.value ? +e.target.value : undefined })
              }
            >
              <option value="">Any</option>
              {PLAYER_OPTIONS.map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
          </label>

          <label>
            Max Player Age
            <select
              value={filter.maxAge ?? ""}
              onChange={(e) =>
                setFilter({ ...filter, maxAge: e.target.value ? +e.target.value : undefined })
              }
            >
              <option value="">Any</option>
              {AGE_OPTIONS.map((n) => (
                <option key={n} value={n}>{n}+</option>
              ))}
            </select>
          </label>

          <label>
            Max Runtime (min)
            <select
              value={filter.maxRuntime ?? ""}
              onChange={(e) =>
                setFilter({ ...filter, maxRuntime: e.target.value ? +e.target.value : undefined })
              }
            >
              <option value="">Any</option>
              {RUNTIME_OPTIONS.map((n) => (
                <option key={n} value={n}>{n} min</option>
              ))}
            </select>
          </label>

          <label>
            Type
            <select
              value={filter.type ?? ""}
              onChange={(e) =>
                setFilter({ ...filter, type: e.target.value || undefined })
              }
            >
              <option value="">Any</option>
              {types.map((t) => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </label>

          <label>
            Category
            <select
              value={filter.category ?? ""}
              onChange={(e) =>
                setFilter({ ...filter, category: e.target.value || undefined })
              }
            >
              <option value="">Any</option>
              {categories.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="filter-actions">
          <button className="btn-primary" onClick={handlePickGame} disabled={loading}>
            {loading ? "Picking…" : "🎲 Pick Random Game"}
          </button>
          <button className="btn-secondary" onClick={handleShowMatches} disabled={loading}>
            {loading ? "Loading…" : "📋 Show All Matches"}
          </button>
          <button className="btn-reset" onClick={resetFilter}>
            Reset
          </button>
        </div>
      </section>

      {error && <div className="error">{error}</div>}

      {randomGame && (
        <section className="result">
          <h2>Tonight you're playing…</h2>
          <GameCard game={randomGame} featured />
          <button className="btn-secondary" onClick={handlePickGame} disabled={loading}>
            🔀 Pick Again
          </button>
        </section>
      )}

      {showList && (
        <section className="game-list">
          <div className="list-header">
            <h2>{matchingGames.length} game{matchingGames.length !== 1 ? "s" : ""} match your filters</h2>
            <button className="btn-reset" onClick={() => setShowList(false)}>Close</button>
          </div>
          <div className="game-grid">
            {matchingGames.map((g) => (
              <GameCard key={g.id} game={g} />
            ))}
          </div>
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
        <div className="game-rank">BGG #{game.bggRank}</div>
        <h3>{game.name}</h3>
        <p className="description">{game.description}</p>
        <div className="game-meta">
          <span title="Players">
            👥 {game.minPlayers === game.maxPlayers ? game.minPlayers : `${game.minPlayers}–${game.maxPlayers}`}
          </span>
          <span title="Runtime">
            ⏱ {game.minRuntime === game.maxRuntime ? game.minRuntime : `${game.minRuntime}–${game.maxRuntime}`} min
          </span>
          <span title="Age">🎂 {game.minAge}+</span>
          <span className="tag">{game.type}</span>
          <span className="tag">{game.category}</span>
        </div>
      </div>
    </div>
  );
}
