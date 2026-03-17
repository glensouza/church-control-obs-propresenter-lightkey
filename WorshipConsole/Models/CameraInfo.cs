namespace WorshipConsole.Models;

public class CameraInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int ViscaPort { get; set; } = 5678;
    public int? UniFiPortNumber { get; set; }
    public int PanSpeed { get; set; } = 10;
    public int TiltSpeed { get; set; } = 10;
    public int ZoomSpeed { get; set; } = 4;
    public int NumberOfPresets { get; set; } = 9;
}
