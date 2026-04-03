using System.ComponentModel.DataAnnotations;

namespace WorshipConsole.Models;

public class PageantCameraPreset
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CameraId { get; set; } // 1, 2, 3, or 4

    [Required]
    [Range(1, 99)]
    public int PresetNumber { get; set; }
}
