using System.ComponentModel.DataAnnotations;

namespace WorshipConsole.Models;

public class PageantObsScene
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; // Friendly name (e.g., "Background A")

    [Required]
    public string ObsSceneName { get; set; } = string.Empty; // Actual OBS scene name
}
