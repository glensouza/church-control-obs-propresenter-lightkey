using System.ComponentModel.DataAnnotations;

namespace WorshipConsole.Models;

public class Settings
{
    public int Id { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
