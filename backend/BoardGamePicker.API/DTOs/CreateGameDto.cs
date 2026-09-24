using System.ComponentModel.DataAnnotations;

namespace BoardGamePicker.API.DTOs;

public class CreateGameDto : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 20)]
    public int MinPlayers { get; set; }

    [Range(1, 20)]
    public int MaxPlayers { get; set; }

    [Range(0, 600)]
    public int MinRuntime { get; set; }

    [Range(0, 600)]
    public int MaxRuntime { get; set; }

    [Range(0, 21)]
    public int MinAge { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int BggRank { get; set; }

    public bool IsOwned { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPlayers > MaxPlayers)
            yield return new ValidationResult(
                "MinPlayers cannot be greater than MaxPlayers.",
                [nameof(MinPlayers), nameof(MaxPlayers)]);

        if (MinRuntime > MaxRuntime)
            yield return new ValidationResult(
                "MinRuntime cannot be greater than MaxRuntime.",
                [nameof(MinRuntime), nameof(MaxRuntime)]);
    }
}
