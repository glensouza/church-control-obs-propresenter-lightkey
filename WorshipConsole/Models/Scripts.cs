using System.ComponentModel.DataAnnotations;

namespace WorshipConsole.Models;

public class Scripts
{
    public int Id { get; set; }

    [Required]
    public string Act { get; set; } = string.Empty;

    [Required]
    public string Scene { get; set; } = string.Empty;

    [Required]
    public int SceneLength { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string SwitchToScene { get; set; } = string.Empty;

    [Required]
    public string Camera1Action { get; set; } = string.Empty;

    public string Camera1Position { get; set; } = string.Empty;

    [Required]
    public string Camera2Action { get; set; } = string.Empty;

    public string Camera2Position { get; set; } = string.Empty;

    [Required]
    public string Camera3Action { get; set; } = string.Empty;

    public string Camera3Position { get; set; } = string.Empty;

    public string NavClass { get; set; } = string.Empty;

    public string StageLightScene { get; set; } = string.Empty;
    public string SpotlightLeft { get; set; } = string.Empty;
    public string SpotlightRight { get; set; } = string.Empty;
    public string HouseLights { get; set; } = string.Empty;
    public string LightingNotes { get; set; } = string.Empty;
}
