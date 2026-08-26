using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

public class XgmMobileControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<XgmMobileControl>? _logger;
    private bool _disposed;

    public bool IsConnected { get; private set; }

    public XgmMobileControl(AsusAcpi acpi, ILogger<XgmMobileControl>? logger = null)
    {
        _acpi = acpi;
        _logger = logger;
    }

    public bool Connect()
    {
        try
        {
            _logger?.LogInformation("Connecting to XG Mobile...");
            int result = _acpi.DeviceSet(AsusDevice.GPUMux, 1, "XgmConnect");
            IsConnected = result == 1;
            _logger?.LogInformation("XG Mobile connection: {Result}", IsConnected ? "success" : "failed");
            return IsConnected;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect XG Mobile");
            return false;
        }
    }

    public bool Disconnect()
    {
        try
        {
            _logger?.LogInformation("Disconnecting XG Mobile...");
            int result = _acpi.DeviceSet(AsusDevice.GPUMux, 0, "XgmDisconnect");
            IsConnected = false;
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to disconnect XG Mobile");
            return false;
        }
    }

    public bool SetRefreshRate(int rate)
    {
        try
        {
            _logger?.LogInformation("Setting XG Mobile refresh rate to {Rate}Hz", rate);
            int result = _acpi.DeviceSet(AsusDevice.ScreenFHD, rate, $"XgmRefreshRate({rate})");
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set XG Mobile refresh rate");
            return false;
        }
    }

    public bool SetPowerLimit(int watts)
    {
        try
        {
            _logger?.LogInformation("Setting XG Mobile power limit to {Watts}W", watts);
            int result = _acpi.DeviceSet(AsusDevice.PPT_GPUC0, watts, $"XgmPowerLimit({watts}W)");
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set XG Mobile power limit");
            return false;
        }
    }

    public (int gpuTemp, int gpuPower) GetStatus()
    {
        try
        {
            int temp = _acpi.DeviceGet(AsusDevice.Temp_GPU);
            int power = _acpi.DeviceGet(AsusDevice.PPT_GPUC0);
            return (temp, power);
        }
        catch
        {
            return (0, 0);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (IsConnected) Disconnect();
            _disposed = true;
        }
    }
}
