using System.ComponentModel.DataAnnotations;

namespace WorshipConsole.Models;

public class Settings
{
    public int Id { get; set; }

    [Required]
    public string SettingType { get; set; } = string.Empty;

    [Required]
    public string SettingId { get; set; } = string.Empty;

    [Required]
    public string Setting { get; set; } = string.Empty;
}
