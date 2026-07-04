namespace BoardGamePicker.API.Models;

public class BoardGame
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
    public bool IsOwned { get; set; } = true;
    public int? UserId { get; set; }
    public User? User { get; set; }
}
