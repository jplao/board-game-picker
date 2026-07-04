using BoardGamePicker.API.Data;
using BoardGamePicker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardGamePicker.Tests.Helpers;

public static class DbContextFactory
{
    /// <summary>
    /// Creates an isolated in-memory AppDbContext pre-seeded with the given games.
    /// Each call gets a unique database name so tests don't bleed into each other.
    /// </summary>
    public static AppDbContext Create(IEnumerable<BoardGame>? seed = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new AppDbContext(options);
        if (seed is not null)
        {
            ctx.BoardGames.AddRange(seed);
            ctx.SaveChanges();
        }
        return ctx;
    }

    public static BoardGame MakeGame(
        int id = 0,
        string name = "Test Game",
        int minPlayers = 2,
        int maxPlayers = 4,
        int minRuntime = 30,
        int maxRuntime = 60,
        int minAge = 10,
        string type = "Strategy",
        string category = "Worker Placement",
        int bggRank = 1,
        bool isOwned = true) => new()
    {
        Id = id,
        Name = name,
        MinPlayers = minPlayers,
        MaxPlayers = maxPlayers,
        MinRuntime = minRuntime,
        MaxRuntime = maxRuntime,
        MinAge = minAge,
        Type = type,
        Category = category,
        Description = "A test game",
        BggRank = bggRank,
        IsOwned = isOwned,
    };
}
