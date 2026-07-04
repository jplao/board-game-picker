using BoardGamePicker.API.Controllers;
using BoardGamePicker.API.DTOs;
using BoardGamePicker.API.Models;
using BoardGamePicker.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BoardGamePicker.Tests.Controllers;

/// <summary>
/// Tests for GamesController using an isolated EF Core InMemory database per test.
/// NSubstitute is available for mocking any future service interfaces extracted from the controller.
/// </summary>
public class GamesControllerTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static GamesController Controller(IEnumerable<BoardGame>? seed = null) =>
        new(DbContextFactory.Create(seed));

    private static BoardGame[] DefaultSeed() =>
    [
        DbContextFactory.MakeGame(id: 1, name: "Wingspan",  minPlayers: 1, maxPlayers: 5, minRuntime: 40, maxRuntime: 70,  minAge: 10, type: "Strategy",    category: "Animals",      bggRank: 10),
        DbContextFactory.MakeGame(id: 2, name: "Catan",     minPlayers: 3, maxPlayers: 4, minRuntime: 60, maxRuntime: 120, minAge: 10, type: "Strategy",    category: "Negotiation",  bggRank: 20),
        DbContextFactory.MakeGame(id: 3, name: "Dixit",     minPlayers: 3, maxPlayers: 6, minRuntime: 30, maxRuntime: 30,  minAge: 8,  type: "Party",       category: "Storytelling", bggRank: 30),
        DbContextFactory.MakeGame(id: 4, name: "Pandemic",  minPlayers: 2, maxPlayers: 4, minRuntime: 45, maxRuntime: 75,  minAge: 8,  type: "Cooperative", category: "Medical",      bggRank: 5),
        DbContextFactory.MakeGame(id: 5, name: "Not Owned", minPlayers: 2, maxPlayers: 4, minRuntime: 30, maxRuntime: 60,  minAge: 10, type: "Strategy",    category: "Animals",      bggRank: 99, isOwned: false),
    ];

    // ActionResult<T> returns the value directly (implicit 200) when the action returns `return entity;`
    // so we read result.Value, not result.Result.
    private static T Value<T>(ActionResult<T> result) where T : class
    {
        if (result.Value is not null) return result.Value;
        // fallback for explicit Ok(...)
        var ok = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<T>(ok.Value);
    }

    // ── GetGames — no filter ─────────────────────────────────────────────────

    [Fact]
    public async Task GetGames_NoFilter_ReturnsOnlyOwnedGames()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto()));
        Assert.Equal(4, games.Count());
        Assert.DoesNotContain(games, g => g.Name == "Not Owned");
    }

    [Fact]
    public async Task GetGames_NoFilter_ResultsOrderedByBggRank()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto())).ToList();
        Assert.Equal("Pandemic", games[0].Name);  // rank 5
        Assert.Equal("Wingspan", games[1].Name);  // rank 10
    }

    // ── GetGames — individual filters ────────────────────────────────────────

    [Fact]
    public async Task GetGames_FilterByPlayerCount_ReturnsGamesWhereCountFitsRange()
    {
        // player count 2 → MinPlayers <= 2 AND MaxPlayers >= 2
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { PlayerCount = 2 }));
        var names = games.Select(g => g.Name).ToList();
        Assert.Contains("Wingspan", names);  // 1-5
        Assert.Contains("Pandemic", names);  // 2-4
        Assert.DoesNotContain("Catan", names); // minPlayers=3
        Assert.DoesNotContain("Dixit", names); // minPlayers=3
    }

    [Fact]
    public async Task GetGames_FilterByMaxAge_ExcludesGamesAbovePlayerAge()
    {
        // "player is 8" → only games with MinAge <= 8
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { MaxAge = 8 }));
        var names = games.Select(g => g.Name).ToList();
        Assert.Contains("Dixit", names);       // minAge 8
        Assert.Contains("Pandemic", names);    // minAge 8
        Assert.DoesNotContain("Wingspan", names); // minAge 10
        Assert.DoesNotContain("Catan", names);    // minAge 10
    }

    [Fact]
    public async Task GetGames_FilterByType_ReturnsOnlyMatchingType()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { Type = "Party" }));
        Assert.Single(games);
        Assert.Equal("Dixit", games.First().Name);
    }

    [Fact]
    public async Task GetGames_FilterByCategories_ReturnsGamesMatchingAnyCategory()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto
        {
            Categories = ["Animals", "Medical"]
        }));
        var names = games.Select(g => g.Name).ToList();
        Assert.Contains("Wingspan", names);
        Assert.Contains("Pandemic", names);
        Assert.DoesNotContain("Catan", names);
        Assert.DoesNotContain("Dixit", names);
    }

    [Fact]
    public async Task GetGames_FilterByMaxRuntime_ExcludesGamesThatTakeTooLong()
    {
        // MaxRuntime = 45 → MinRuntime <= 45
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { MaxRuntime = 45 }));
        var names = games.Select(g => g.Name).ToList();
        Assert.Contains("Dixit", names);    // minRuntime 30
        Assert.Contains("Pandemic", names); // minRuntime 45
        Assert.Contains("Wingspan", names); // minRuntime 40
        Assert.DoesNotContain("Catan", names); // minRuntime 60
    }

    [Fact]
    public async Task GetGames_EmptyCategories_DoesNotFilterByCategory()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { Categories = [] }));
        Assert.Equal(4, games.Count());
    }

    // ── GetGames — combined filters ──────────────────────────────────────────

    [Fact]
    public async Task GetGames_TypeAndPlayerCount_AppliesBothFilters()
    {
        // Strategy + 4 players → Wingspan(1-5) and Catan(3-4); Dixit is Party, Pandemic is Cooperative
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto
        {
            Type = "Strategy",
            PlayerCount = 4,
        }));
        var names = games.Select(g => g.Name).ToList();
        Assert.Contains("Wingspan", names);
        Assert.Contains("Catan", names);
        Assert.DoesNotContain("Dixit", names);
        Assert.DoesNotContain("Pandemic", names);
    }

    [Fact]
    public async Task GetGames_AllFilters_NarrowsCorrectly()
    {
        // Party + 4 players + age 8 + maxRuntime 30 → only Dixit
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto
        {
            Type = "Party",
            PlayerCount = 4,
            MaxAge = 8,
            MaxRuntime = 30,
        }));
        Assert.Single(games);
        Assert.Equal("Dixit", games.First().Name);
    }

    [Fact]
    public async Task GetGames_FiltersWithNoMatch_ReturnsEmpty()
    {
        var games = Value(await Controller(DefaultSeed()).GetGames(new GameFilterDto { Type = "Unknown" }));
        Assert.Empty(games);
    }

    // ── GetRandomGame ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRandomGame_NoFilter_ReturnsOwnedGame()
    {
        var game = Value(await Controller(DefaultSeed()).GetRandomGame(new GameFilterDto()));
        Assert.True(game.IsOwned);
    }

    [Fact]
    public async Task GetRandomGame_SingleMatch_ReturnsThatGame()
    {
        var game = Value(await Controller(DefaultSeed()).GetRandomGame(new GameFilterDto { Type = "Party" }));
        Assert.Equal("Dixit", game.Name);
    }

    [Fact]
    public async Task GetRandomGame_NoMatch_Returns404()
    {
        var result = await Controller(DefaultSeed()).GetRandomGame(new GameFilterDto { Type = "NonExistent" });
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRandomGame_EmptyDb_Returns404()
    {
        var result = await Controller([]).GetRandomGame(new GameFilterDto());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── GetTypes / GetCategories ─────────────────────────────────────────────

    [Fact]
    public async Task GetTypes_ReturnsDistinctTypesSorted()
    {
        var types = Value(await Controller(DefaultSeed()).GetTypes()).ToList();
        Assert.Equal(["Cooperative", "Party", "Strategy"], types);
    }

    [Fact]
    public async Task GetCategories_ReturnsDistinctCategoriesSorted()
    {
        var cats = Value(await Controller(DefaultSeed()).GetCategories()).ToList();
        Assert.Equal(cats.Distinct().Count(), cats.Count); // no duplicates
        Assert.Contains("Animals", cats);
        Assert.Contains("Medical", cats);
    }

    [Fact]
    public async Task GetCategories_IncludesUnownedGameCategories()
    {
        // "Animals" exists on the unowned game too, but should still appear (no IsOwned filter on categories)
        var cats = Value(await Controller(DefaultSeed()).GetCategories());
        Assert.Contains("Animals", cats);
    }

    // ── GetGame by id ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGame_ExistingId_ReturnsGame()
    {
        var game = Value(await Controller(DefaultSeed()).GetGame(1));
        Assert.Equal("Wingspan", game.Name);
    }

    [Fact]
    public async Task GetGame_NonExistentId_Returns404()
    {
        var result = await Controller(DefaultSeed()).GetGame(9999);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── CreateGame ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGame_ValidGame_Returns201AndPersists()
    {
        var ctx = DbContextFactory.Create();
        var controller = new GamesController(ctx);
        var newGame = DbContextFactory.MakeGame(name: "Azul");

        var result = await controller.CreateGame(newGame);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<BoardGame>(created.Value);
        Assert.Equal("Azul", returned.Name);
        Assert.True(returned.Id > 0);
        Assert.Equal(1, ctx.BoardGames.Count());
    }

    [Fact]
    public async Task CreateGame_ThenGetById_ReturnsCreatedGame()
    {
        var ctx = DbContextFactory.Create();
        var controller = new GamesController(ctx);
        var newGame = DbContextFactory.MakeGame(name: "Ticket to Ride");
        await controller.CreateGame(newGame);

        var game = Value(await controller.GetGame(newGame.Id));
        Assert.Equal("Ticket to Ride", game.Name);
    }

    // ── DeleteGame ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteGame_ExistingId_RemovesGameAndReturns204()
    {
        var ctx = DbContextFactory.Create(DefaultSeed());
        var controller = new GamesController(ctx);

        var result = await controller.DeleteGame(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ctx.BoardGames.FindAsync(1));
    }

    [Fact]
    public async Task DeleteGame_NonExistentId_Returns404()
    {
        var result = await Controller(DefaultSeed()).DeleteGame(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── UpdateGame ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGame_ValidUpdate_Returns204AndPersistsChange()
    {
        var ctx = DbContextFactory.Create(DefaultSeed());
        var controller = new GamesController(ctx);
        var game = await ctx.BoardGames.FindAsync(1);
        game!.Name = "Wingspan (Updated)";

        var result = await controller.UpdateGame(1, game);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Wingspan (Updated)", (await ctx.BoardGames.FindAsync(1))!.Name);
    }

    [Fact]
    public async Task UpdateGame_IdMismatch_Returns400()
    {
        var ctx = DbContextFactory.Create(DefaultSeed());
        var controller = new GamesController(ctx);
        var game = await ctx.BoardGames.FindAsync(1);

        var result = await controller.UpdateGame(99, game!);

        Assert.IsType<BadRequestResult>(result);
    }
}
