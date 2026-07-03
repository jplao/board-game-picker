using BoardGamePicker.API.Data;
using BoardGamePicker.API.DTOs;
using BoardGamePicker.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGamePicker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(AppDbContext db) : ControllerBase
{
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardGame>>> GetGames([FromQuery] GameFilterDto filter)
    {
        var query = ApplyFilters(db.BoardGames.Where(g => g.IsOwned), filter);
        return await query.OrderBy(g => g.BggRank).ToListAsync();
    }

    [HttpGet("random")]
    public async Task<ActionResult<BoardGame>> GetRandomGame([FromQuery] GameFilterDto filter)
    {
        var query = ApplyFilters(db.BoardGames.Where(g => g.IsOwned), filter);

        var count = await query.CountAsync();
        if (count == 0) return NotFound("No games match the selected criteria.");

        var skip = Random.Shared.Next(count);
        var game = await query.Skip(skip).FirstAsync();
        return game;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardGame>> GetGame(int id)
    {
        var game = await db.BoardGames.FindAsync(id);
        return game is null ? NotFound() : game;
    }

    [HttpPost]
    public async Task<ActionResult<BoardGame>> CreateGame(BoardGame game)
    {
        db.BoardGames.Add(game);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGame(int id, BoardGame game)
    {
        if (id != game.Id) return BadRequest();
        db.Entry(game).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await db.BoardGames.FindAsync(id);
        if (game is null) return NotFound();
        db.BoardGames.Remove(game);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<string>>> GetTypes()
        => await db.BoardGames.Select(g => g.Type).Distinct().OrderBy(t => t).ToListAsync();

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        => await db.BoardGames.Select(g => g.Category).Distinct().OrderBy(c => c).ToListAsync();
}
