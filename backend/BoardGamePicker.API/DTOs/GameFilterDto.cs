namespace BoardGamePicker.API.DTOs;

public class GameFilterDto
{
    public int? PlayerCount { get; set; }
    public int? MaxAge { get; set; }
    public string? Type { get; set; }
    public string[]? Categories { get; set; }
    public int? MaxRuntime { get; set; }
}
