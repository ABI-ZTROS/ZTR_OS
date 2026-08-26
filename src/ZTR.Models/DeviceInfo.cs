namespace ZTR.Models;

/// <summary>
/// Contains information about the detected ASUS device and its capabilities.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The hardware model name (e.g., "ROG Strix G16").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The BIOS version string.
    /// </summary>
    public string BiosVersion { get; set; } = string.Empty;

    /// <summary>
    /// The manufacturer name.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// The type of device.
    /// </summary>
    public DeviceType Type { get; set; }

    /// <summary>
    /// The detected CPU model name.
    /// </summary>
    public string CpuModel { get; set; } = string.Empty;

    /// <summary>
    /// The detected GPU model name.
    /// </summary>
    public string GpuModel { get; set; } = string.Empty;

    /// <summary>
    /// The supported features list (e.g., "Aura", "FanControl", "GPUModes").
    /// </summary>
    public IReadOnlyList<string> SupportedFeatures { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The number of CPU fans detected.
    /// </summary>
    public int CpuFanCount { get; set; }

    /// <summary>
    /// The number of GPU fans detected.
    /// </summary>
    public int GpuFanCount { get; set; }

    /// <summary>
    /// Whether the device supports Aura RGB control.
    /// </summary>
    public bool SupportsAura { get; set; }

    /// <summary>
    /// Whether the device supports fan curve control.
    /// </summary>
    public bool SupportsFanControl { get; set; }

    /// <summary>
    /// Whether the device supports GPU mode switching (Eco/Standard/Ultimate).
    /// </summary>
    public bool SupportsGpuModes { get; set; }

    /// <summary>
    /// Whether the device supports battery charge limiting.
    /// </summary>
    public bool SupportsBatteryLimit { get; set; }

    /// <summary>
    /// Whether the device supports performance mode switching.
    /// </summary>
    public bool SupportsPerformanceModes { get; set; }

    /// <summary>
    /// Whether the ATKACPI driver is available and working.
    /// </summary>
    public bool IsAtkAcpiAvailable { get; set; }
}

/// <summary>
/// The type of ASUS device.
/// </summary>
public enum DeviceType
{
    Unknown,
    Laptop,
    Desktop,
    Ally,
    Tablet
}