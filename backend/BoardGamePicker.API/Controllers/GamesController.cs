using System.Security.Claims;
using BoardGamePicker.API.Data;
using BoardGamePicker.API.DTOs;
using BoardGamePicker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGamePicker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(AppDbContext db) : ControllerBase
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool IsAdmin => User.IsInRole("admin");

    private IQueryable<BoardGame> ScopedGames()
    {
        var userId = CurrentUserId;
        // Admins see everything; authenticated users see only their own games;
        // unauthenticated requests see nothing (all endpoints require auth now).
        return IsAdmin
            ? db.BoardGames
            : db.BoardGames.Where(g => g.UserId == userId);
    }

    private IQueryable<BoardGame> ApplyFilters(IQueryable<BoardGame> query, GameFilterDto filter)
    {
        if (filter.PlayerCount.HasValue)
        {
            var pc = filter.PlayerCount.Value;
            query = query.Where(g => g.MinPlayers <= pc && g.MaxPlayers >= pc);
        }
        if (filter.MaxAge.HasValue)
        {
            var age = filter.MaxAge.Value;
            query = query.Where(g => g.MinAge <= age);
        }
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            var type = filter.Type;
            query = query.Where(g => g.Type == type);
        }
        if (filter.Categories is { Length: > 0 })
        {
            var cats = filter.Categories;
            query = query.Where(g => cats.Contains(g.Category));
        }
        if (filter.MaxRuntime.HasValue)
        {
            var runtime = filter.MaxRuntime.Value;
            query = query.Where(g => g.MinRuntime <= runtime);
        }
        return query;
    }

    // ── read endpoints (authenticated users see their own games) ─────────────

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<GameDto>>> GetGames([FromQuery] GameFilterDto filter)
    {
        var query = ApplyFilters(ScopedGames().Where(g => g.IsOwned), filter);
        var games = await query.OrderBy(g => g.BggRank).ToListAsync();
        return games.Select(GameDto.From).ToList();
    }

    [HttpGet("random")]
    [Authorize]
    public async Task<ActionResult<GameDto>> GetRandomGame([FromQuery] GameFilterDto filter)
    {
        var query = ApplyFilters(ScopedGames().Where(g => g.IsOwned), filter);
        // Load all matching IDs in one query, then pick one — avoids the
        // count-then-skip race where a deletion between the two queries
        // could cause Skip(n) to go out of bounds.
        var ids = await query.Select(g => g.Id).ToListAsync();
        if (ids.Count == 0) return NotFound("No games match the selected criteria.");
        var randomId = ids[Random.Shared.Next(ids.Count)];
        var game = await db.BoardGames.FindAsync(randomId);
        return game is null ? NotFound("No games match the selected criteria.") : GameDto.From(game);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<GameDto>> GetGame(int id)
    {
        var game = await ScopedGames().FirstOrDefaultAsync(g => g.Id == id);
        return game is null ? NotFound() : GameDto.From(game);
    }

    [HttpGet("types")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> GetTypes()
        => await ScopedGames().Select(g => g.Type).Distinct().OrderBy(t => t).ToListAsync();

    [HttpGet("categories")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        => await ScopedGames().Select(g => g.Category).Distinct().OrderBy(c => c).ToListAsync();

    // ── write endpoints (own games only, or admin) ───────────────────────────

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<GameDto>> CreateGame(CreateGameDto dto)
    {
        var game = new BoardGame
        {
            Name        = dto.Name,
            MinPlayers  = dto.MinPlayers,
            MaxPlayers  = dto.MaxPlayers,
            MinRuntime  = dto.MinRuntime,
            MaxRuntime  = dto.MaxRuntime,
            MinAge      = dto.MinAge,
            ImageUrl    = dto.ImageUrl,
            Description = dto.Description,
            Type        = dto.Type,
            Category    = dto.Category,
            BggRank     = dto.BggRank,
            IsOwned     = dto.IsOwned,
            UserId      = CurrentUserId,
        };
        db.BoardGames.Add(game);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, GameDto.From(game));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateGame(int id, CreateGameDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var existing = await db.BoardGames.FindAsync(id);
        if (existing is null) return NotFound();
        if (!IsAdmin && existing.UserId != CurrentUserId) return Forbid();

        existing.Name        = dto.Name;
        existing.MinPlayers  = dto.MinPlayers;
        existing.MaxPlayers  = dto.MaxPlayers;
        existing.MinRuntime  = dto.MinRuntime;
        existing.MaxRuntime  = dto.MaxRuntime;
        existing.MinAge      = dto.MinAge;
        existing.ImageUrl    = dto.ImageUrl;
        existing.Description = dto.Description;
        existing.Type        = dto.Type;
        existing.Category    = dto.Category;
        existing.BggRank     = dto.BggRank;
        existing.IsOwned     = dto.IsOwned;
        // UserId is intentionally not updated — ownership cannot be reassigned

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await db.BoardGames.FindAsync(id);
        if (game is null) return NotFound();
        if (!IsAdmin && game.UserId != CurrentUserId) return Forbid();
        db.BoardGames.Remove(game);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
