using System.Net.Sockets;

namespace WorshipConsole.Services;

public enum PanTiltDirection
{
    Stop,
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}

public enum ZoomDirection
{
    Stop,
    In,
    Out
}

public enum FocusDirection
{
    Stop,
    Far,
    Near
}

public class ViscaService
{
    private readonly ILogger<ViscaService> _logger;
    private const int TimeoutMs = 2000;

    public ViscaService(ILogger<ViscaService> logger)
    {
        _logger = logger;
    }

    private async Task<bool> SendCommandAsync(string ipAddress, int port, byte[] command)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeoutMs);
            await client.ConnectAsync(ipAddress, port, cts.Token);
            var stream = client.GetStream();
            stream.WriteTimeout = TimeoutMs;
            stream.ReadTimeout = TimeoutMs;
            await stream.WriteAsync(command, cts.Token);
            await stream.FlushAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("VISCA command to {Ip}:{Port} timed out", ipAddress, port);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending VISCA command to {Ip}:{Port}", ipAddress, port);
            return false;
        }
    }

    public Task<bool> PanTiltAsync(string ipAddress, int port, PanTiltDirection direction, int panSpeed = 10, int tiltSpeed = 10)
    {
        byte ps = (byte)Math.Clamp(panSpeed, 1, 0x18);
        byte ts = (byte)Math.Clamp(tiltSpeed, 1, 0x17);

        byte panDir;
        byte tiltDir;

        switch (direction)
        {
            case PanTiltDirection.Up:        panDir = 0x03; tiltDir = 0x01; break;
            case PanTiltDirection.Down:      panDir = 0x03; tiltDir = 0x02; break;
            case PanTiltDirection.Left:      panDir = 0x01; tiltDir = 0x03; break;
            case PanTiltDirection.Right:     panDir = 0x02; tiltDir = 0x03; break;
            case PanTiltDirection.UpLeft:    panDir = 0x01; tiltDir = 0x01; break;
            case PanTiltDirection.UpRight:   panDir = 0x02; tiltDir = 0x01; break;
            case PanTiltDirection.DownLeft:  panDir = 0x01; tiltDir = 0x02; break;
            case PanTiltDirection.DownRight: panDir = 0x02; tiltDir = 0x02; break;
            default:                         panDir = 0x03; tiltDir = 0x03; ps = 0x00; ts = 0x00; break;
        }

        byte[] command = [0x81, 0x01, 0x06, 0x01, ps, ts, panDir, tiltDir, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> PanTiltStopAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x06, 0x01, 0x00, 0x00, 0x03, 0x03, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> ZoomAsync(string ipAddress, int port, ZoomDirection direction, int speed = 4)
    {
        byte[] command;
        if (direction == ZoomDirection.Stop)
        {
            command = [0x81, 0x01, 0x04, 0x07, 0x00, 0xFF];
        }
        else
        {
            byte s = (byte)Math.Clamp(speed, 0, 7);
            byte dir = direction == ZoomDirection.In ? (byte)0x20 : (byte)0x30;
            command = [0x81, 0x01, 0x04, 0x07, (byte)(dir | s), 0xFF];
        }
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> ZoomStopAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x04, 0x07, 0x00, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> FocusAsync(string ipAddress, int port, FocusDirection direction)
    {
        byte[] command;
        switch (direction)
        {
            case FocusDirection.Far:
                command = [0x81, 0x01, 0x04, 0x08, 0x02, 0xFF];
                break;
            case FocusDirection.Near:
                command = [0x81, 0x01, 0x04, 0x08, 0x03, 0xFF];
                break;
            default:
                command = [0x81, 0x01, 0x04, 0x08, 0x00, 0xFF];
                break;
        }
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> FocusStopAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x04, 0x08, 0x00, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> FocusAutoAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x04, 0x38, 0x02, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> FocusManualAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x04, 0x38, 0x03, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> RecallPresetAsync(string ipAddress, int port, int presetNumber)
    {
        byte preset = (byte)Math.Clamp(presetNumber, 0, 254);
        byte[] command = [0x81, 0x01, 0x04, 0x3F, 0x02, preset, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> SetPresetAsync(string ipAddress, int port, int presetNumber)
    {
        byte preset = (byte)Math.Clamp(presetNumber, 0, 254);
        byte[] command = [0x81, 0x01, 0x04, 0x3F, 0x01, preset, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }

    public Task<bool> HomeAsync(string ipAddress, int port)
    {
        byte[] command = [0x81, 0x01, 0x06, 0x04, 0xFF];
        return SendCommandAsync(ipAddress, port, command);
    }
}
