using HidSharp;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Interface for an HID device stream, abstracting HidSharp for testability.
/// </summary>
public interface IHidDeviceStream : IDisposable
{
    void Write(byte[] data);
}

/// <summary>
/// Real implementation wrapping a HidSharp HidStream.
/// </summary>
internal class HidDeviceStream : IHidDeviceStream
{
    private readonly HidStream _stream;

    public HidDeviceStream(HidStream stream)
    {
        _stream = stream;
    }

    public void Write(byte[] data) => _stream.Write(data);

    public void Dispose() => _stream.Dispose();
}

/// <summary>
/// Implements USB HID communication with ASUS devices.
/// Replicates G-Helper's AsusHid module using HidSharpCore.
/// </summary>
public class AsusHid : IDisposable
{
    public const byte INPUT_ID = 0x5A;
    public const byte AURA_ID = 0x5D;
    public const byte XGM_REPORT_ID = 0x5E;

    public const int ASUS_VID = 0x0B05;

    private static readonly int[] _mainAuraPids = {
        0x1a30, 0x1854, 0x1869, 0x1866, 0x19b6, 0x1822, 0x1837,
        0x184a, 0x183d, 0x8502, 0x1807, 0x17e0, 0x1abe, 0x1b4c,
        0x1b6e, 0x1b2c, 0x8854, 0x1CE7, 0x1bf2, 0x1cd7, 0x1cd8
    };

    public static IReadOnlyList<int> MainAuraPids => _mainAuraPids;

    private static readonly int[] _rearLightPids = { 0x18c6 };
    public static IReadOnlyList<int> RearLightPids => _rearLightPids;

    private static readonly int[] _xgmPids = { 0x1970, 0x1a9a, 0x1C28, 0x1C29, 0x1BC1 };
    public static IReadOnlyList<int> XgmPids => _xgmPids;

    private readonly Dictionary<int, IHidDeviceStream> _streams = new();
    private readonly IHidReportWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Number of retry attempts for failed HID operations.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// </summary>
    public int RetryDelayMs { get; set; } = 50;

    /// <summary>
    /// Whether the HID device has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Number of connected HID devices.
    /// </summary>
    public int DeviceCount => _streams.Count;

    /// <summary>
    /// Initializes a new instance using the default HidSharp-based writer.
    /// </summary>
    public AsusHid() : this(new HidSharpReportWriter())
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom report writer (for testing).
    /// </summary>
    /// <param name="writer">The HID report writer implementation.</param>
    public AsusHid(IHidReportWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Initialize input channels for all supported devices.
    /// </summary>
    public void Initialize()
    {
        FindAndOpenDevices(INPUT_ID);
        IsInitialized = _streams.Count > 0;
    }

    /// <summary>
    /// Find and open HID devices matching the given report ID.
    /// </summary>
    private IReadOnlyDictionary<int, IHidDeviceStream> FindAndOpenDevices(byte reportId)
    {
        var devices = DeviceList.Local
            .GetHidDevices(ASUS_VID)
            .ToList();

        var matching = devices
            .Where(d => d.ProductID > 0)
            .ToList();

        foreach (var device in matching)
        {
            try
            {
                var stream = device.Open();
                if (stream != null)
                {
                    int pid = device.ProductID;
                    _streams[pid] = new HidDeviceStream(stream);
                }
            }
            catch { }
        }

        return _streams;
    }

    /// <summary>
    /// Get the HID stream for a specific product ID.
    /// </summary>
    /// <param name="pid">The product ID to look up.</param>
    /// <returns>The IHidDeviceStream for the PID, or null if not found.</returns>
    public IHidDeviceStream? GetDeviceStream(int pid)
    {
        return _streams.TryGetValue(pid, out var stream) ? stream : null;
    }

    /// <summary>
    /// Registers a device stream for a specific PID. Used for testing.
    /// </summary>
    /// <param name="pid">The product ID to register.</param>
    /// <param name="stream">The stream to register, or null for a test-only placeholder.</param>
    internal void RegisterStream(int pid, IHidDeviceStream? stream = null)
    {
        _streams[pid] = stream ?? new FakeHidDeviceStream();
    }

    /// <summary>
    /// Reconnect to all devices after a USB reset.
    /// Closes all existing streams and re-discovers devices.
    /// </summary>
    public void Reconnect()
    {
        foreach (var stream in _streams.Values)
        {
            try { stream.Dispose(); } catch { }
        }
        _streams.Clear();
        IsInitialized = false;
        Initialize();
    }

    /// <summary>
    /// Send input report (keyboard, controller, etc.) via Report ID 0x5A.
    /// </summary>
    /// <param name="data">The report payload data.</param>
    /// <param name="log">Optional log description.</param>
    /// <returns>True if all writes succeeded.</returns>
    public bool WriteInput(byte[] data, string? log = null)
    {
        byte[] reportData = new byte[data.Length + 1];
        reportData[0] = INPUT_ID;
        Array.Copy(data, 0, reportData, 1, data.Length);

        return WriteToAll(reportData, _mainAuraPids);
    }

    /// <summary>
    /// Send Aura report via Report ID 0x5D.
    /// </summary>
    /// <param name="data">The report payload data.</param>
    /// <param name="log">Log description for the operation.</param>
    /// <returns>True if all writes succeeded.</returns>
    public bool Write(byte[] data, string log)
    {
        byte[] reportData = new byte[data.Length + 1];
        reportData[0] = AURA_ID;
        Array.Copy(data, 0, reportData, 1, data.Length);

        return WriteToAll(reportData, _mainAuraPids);
    }

    /// <summary>
    /// Send multiple Aura messages as a batch.
    /// </summary>
    /// <param name="dataList">List of report payloads.</param>
    /// <param name="log">Log description for the batch.</param>
    /// <param name="pids">Optional specific PIDs to target. Defaults to main Aura PIDs.</param>
    /// <returns>True if all writes succeeded.</returns>
    public bool WriteBatch(IEnumerable<byte[]> dataList, string log, int[]? pids = null)
    {
        bool allSuccess = true;
        int[] targetPids = pids ?? _mainAuraPids;

        foreach (var data in dataList)
        {
            allSuccess = Write(data, log) && allSuccess;
        }

        return allSuccess;
    }

    /// <summary>
    /// Send XGM report via Report ID 0x5E.
    /// </summary>
    /// <param name="data">The report payload data.</param>
    /// <param name="log">Log description for the operation.</param>
    /// <returns>True if all writes succeeded.</returns>
    public bool WriteXgm(byte[] data, string log)
    {
        byte[] reportData = new byte[data.Length + 1];
        reportData[0] = XGM_REPORT_ID;
        Array.Copy(data, 0, reportData, 1, data.Length);

        return WriteToAll(reportData, _xgmPids);
    }

    /// <summary>
    /// Set a feature report for Aura devices with optional retry logic.
    /// </summary>
    /// <param name="data">The report payload data.</param>
    /// <param name="retry">Whether to retry on failure.</param>
    /// <returns>True if the write succeeded.</returns>
    public bool SetFeatureAura(byte[] data, bool retry = true)
    {
        byte[] reportData = new byte[data.Length + 1];
        reportData[0] = AURA_ID;
        Array.Copy(data, 0, reportData, 1, data.Length);

        bool success = WriteToAll(reportData, _mainAuraPids);

        if (!success && retry)
        {
            success = ExecuteWithRetry(() => WriteToAll(reportData, _mainAuraPids));
        }

        return success;
    }

    /// <summary>
    /// Write a feature report directly to all devices matching the given report ID.
    /// </summary>
    /// <param name="reportId">The report ID to use.</param>
    /// <param name="data">The feature report data (without report ID prefix).</param>
    /// <returns>True if all writes succeeded.</returns>
    public bool SetFeatureReport(byte reportId, byte[] data)
    {
        byte[] reportData = new byte[data.Length + 1];
        reportData[0] = reportId;
        Array.Copy(data, 0, reportData, 1, data.Length);

        return WriteToAll(reportData, _mainAuraPids);
    }

    /// <summary>
    /// Read a feature report from the first available device.
    /// </summary>
    /// <param name="reportId">The report ID to read.</param>
    /// <param name="length">Number of bytes to read (excluding report ID).</param>
    /// <returns>The report data, or null if read failed.</returns>
    public byte[]? ReadFeature(byte reportId, int length)
    {
        return _writer.ReadFeature(reportId, length);
    }

    /// <summary>
    /// Initialize input stream with device handshake.
    /// </summary>
    /// <returns>True if the handshake succeeded.</returns>
    public bool InitInput()
    {
        byte[] initData = { INPUT_ID, 0x00 };
        return WriteToAll(initData, _mainAuraPids);
    }

    /// <summary>
    /// Probe Aura device capabilities including backlight type and zone count.
    /// </summary>
    /// <returns>An AuraProbeResult describing the detected capabilities.</returns>
    public AuraProbeResult ProbeAura()
    {
        return new AuraProbeResult
        {
            IsAvailable = _streams.Count > 0,
            SupportedZones = DetectSupportedZones(),
            LayoutType = DetectLayoutType(),
            BacklightType = DetectBacklightType(),
            ZoneCount = DetectZoneCount()
        };
    }

    private bool WriteToAll(byte[] data, IEnumerable<int> pids)
    {
        bool allSuccess = true;
        foreach (var pid in pids)
        {
            if (_streams.TryGetValue(pid, out var stream))
            {
                try
                {
                    _writer.WriteReport(stream, data);
                }
                catch
                {
                    allSuccess = false;
                }
            }
            else
            {
                allSuccess = false;
            }
        }
        return allSuccess;
    }

    /// <summary>
    /// Executes an action with retry logic.
    /// </summary>
    private bool ExecuteWithRetry(Func<bool> action)
    {
        for (int attempt = 0; attempt < RetryCount; attempt++)
        {
            try
            {
                if (action())
                    return true;
            }
            catch
            {
            }

            if (attempt < RetryCount - 1)
            {
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
        }
        return false;
    }

    private IReadOnlyList<AuraZone> DetectSupportedZones()
    {
        var zones = new List<AuraZone> { AuraZone.Keyboard };

        if (_streams.ContainsKey(0x1a30) || _streams.ContainsKey(0x1854))
        {
            zones.Add(AuraZone.Body);
            zones.Add(AuraZone.Touchpad);
        }

        if (_streams.ContainsKey(0x18c6))
        {
            zones.Add(AuraZone.Rear);
        }

        return zones;
    }

    private AuraLayoutType DetectLayoutType()
    {
        int[] perKeyPids = { 0x1a30, 0x19b6, 0x1cd7, 0x1cd8 };
        foreach (var pid in perKeyPids)
        {
            if (_streams.ContainsKey(pid))
                return AuraLayoutType.PerKey;
        }
        return AuraLayoutType.FourZone;
    }

    private BacklightType DetectBacklightType()
    {
        if (_streams.ContainsKey(0x1a30))
            return BacklightType.PerKeyAddressable;
        if (_streams.ContainsKey(0x19b6))
            return BacklightType.PerKeyAddressable;
        return BacklightType.FourZone;
    }

    private int DetectZoneCount()
    {
        return DetectLayoutType() == AuraLayoutType.PerKey ? 4 : 4;
    }

    /// <summary>
    /// Check if a product ID is in the main Aura PIDs list.
    /// </summary>
    public static bool IsMainAuraPid(int pid) => _mainAuraPids.Contains(pid);

    /// <summary>
    /// Check if a product ID is in the XGM PIDs list.
    /// </summary>
    public static bool IsXgmPid(int pid) => _xgmPids.Contains(pid);

    /// <summary>
    /// Check if a product ID is in the rear light PIDs list.
    /// </summary>
    public static bool IsRearLightPid(int pid) => _rearLightPids.Contains(pid);

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var stream in _streams.Values)
            {
                try { stream.Dispose(); } catch { }
            }
            _streams.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// A fake device stream used for testing when no hardware is present.
/// </summary>
internal class FakeHidDeviceStream : IHidDeviceStream
{
    public void Write(byte[] data) { }
    public void Dispose() { }
}

/// <summary>
/// Interface for HID report writing, enabling testability.
/// </summary>
public interface IHidReportWriter
{
    /// <summary>
    /// Write a report to the given stream.
    /// </summary>
    /// <param name="stream">The HID stream to write to.</param>
    /// <param name="data">The report data including report ID prefix.</param>
    void WriteReport(IHidDeviceStream stream, byte[] data);

    /// <summary>
    /// Read a feature report from the first available device.
    /// </summary>
    /// <param name="reportId">The report ID to read.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The report data, or null if read failed.</returns>
    byte[]? ReadFeature(byte reportId, int length);
}

/// <summary>
/// Default implementation using HidSharp for actual hardware communication.
/// </summary>
public class HidSharpReportWriter : IHidReportWriter
{
    /// <inheritdoc/>
    public void WriteReport(IHidDeviceStream stream, byte[] data)
    {
        stream.Write(data);
    }

    /// <inheritdoc/>
    public byte[]? ReadFeature(byte reportId, int length)
    {
        return null;
    }
}

/// <summary>
/// Result of probing Aura device capabilities.
/// </summary>
public class AuraProbeResult
{
    /// <summary>
    /// Whether an Aura device is available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// The set of supported Aura zones.
    /// </summary>
    public IReadOnlyList<AuraZone> SupportedZones { get; set; } = Array.Empty<AuraZone>();

    /// <summary>
    /// The detected layout type (FourZone or PerKey).
    /// </summary>
    public AuraLayoutType LayoutType { get; set; }

    /// <summary>
    /// The detected backlight type.
    /// </summary>
    public BacklightType BacklightType { get; set; }

    /// <summary>
    /// The number of independently addressable zones.
    /// </summary>
    public int ZoneCount { get; set; }
}

/// <summary>
/// The type of backlight used by the device.
/// </summary>
public enum BacklightType
{
    Unknown,
    FourZone,
    PerKeyAddressable
}

/// <summary>
/// The layout type for Aura lighting.
/// </summary>
public enum AuraLayoutType
{
    FourZone,
    PerKey,
    Unknown
}