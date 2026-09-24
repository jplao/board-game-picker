using BoardGamePicker.API.Models;

namespace BoardGamePicker.API.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinRuntime { get; set; }
    public int MaxRuntime { get; set; }
    public int MinAge { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int BggRank { get; set; }
    public bool IsOwned { get; set; }

    public static GameDto From(BoardGame g) => new()
    {
        Id          = g.Id,
        Name        = g.Name,
        MinPlayers  = g.MinPlayers,
        MaxPlayers  = g.MaxPlayers,
        MinRuntime  = g.MinRuntime,
        MaxRuntime  = g.MaxRuntime,
        MinAge      = g.MinAge,
        ImageUrl    = g.ImageUrl,
        Description = g.Description,
        Type        = g.Type,
        Category    = g.Category,
        BggRank     = g.BggRank,
        IsOwned     = g.IsOwned,
    };
}
