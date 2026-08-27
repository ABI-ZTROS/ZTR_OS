using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Implements ACPI communication with ASUS ATK driver via DeviceIoControl.
/// Replicates the G-Helper AsusACPI module functionality with retry logic and logging.
/// </summary>
public class AsusAcpi : IDisposable
{
    private const int MaxRetries = 3;
    private const int BaseRetryDelayMs = 100;
    private const int DefaultBufferSize = 256;

    private readonly IAtkDevice _device;
    private readonly ILogger<AsusAcpi>? _logger;

    /// <summary>
    /// Gets a value indicating whether the ATKACPI device handle is available.
    /// </summary>
    public bool IsAvailable => _device.IsAvailable;
    public string OpenedPath => _device.OpenedPath;

    /// <summary>
    /// Creates a new instance of the <see cref="AsusAcpi"/> class using the default ATK device.
    /// </summary>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public AsusAcpi(ILogger<AsusAcpi>? logger = null)
        : this(new AtkDevice(), logger)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AsusAcpi"/> class with a specified device implementation.
    /// </summary>
    /// <param name="device">The ATK device implementation to use.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public AsusAcpi(IAtkDevice device, ILogger<AsusAcpi>? logger = null)
    {
        _device = device;
        _logger = logger;

        if (!device.IsAvailable)
        {
            _logger?.LogWarning("ATKACPI device handle could not be opened. ASUS ACPI functionality will be unavailable.");
        }
    }

    /// <summary>
    /// Writes a device value via ACPI.
    /// G-Helper protocol: returns status code (1=OK).
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="status">The status value to write.</param>
    /// <param name="logName">Optional descriptive name for logging.</param>
    /// <returns>Status code (1=OK), or -1 on failure.</returns>
    public int DeviceSet(AsusDevice deviceId, int status, string? logName = null)
    {
        if (!IsAvailable) return -1;

        var method = DeviceMethod.DEVS;
        byte[] args = BitConverter.GetBytes((uint)deviceId)
            .Concat(BitConverter.GetBytes((uint)status))
            .ToArray();

        byte[] result = CallMethodBufferWithRetry(method, args, logName ?? $"DeviceSet({deviceId})");
        if (result.Length >= 4)
        {
            int code = BitConverter.ToInt32(result, 0);
            _logger?.LogDebug("{Operation} = {Status} : {Code}", logName ?? $"DeviceSet({deviceId})", status, code);
            return code;
        }
        return -1;
    }

    /// <summary>
    /// Writes device bytes via ACPI.
    /// G-Helper protocol: returns full output buffer.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="parameters">The raw parameter bytes to write.</param>
    /// <param name="logName">Optional descriptive name for logging.</param>
    /// <returns>The output buffer bytes, or empty array on failure.</returns>
    public byte[] DeviceSet(AsusDevice deviceId, byte[] parameters, string? logName = null)
    {
        if (!IsAvailable) return Array.Empty<byte>();

        var method = DeviceMethod.DEVS;
        byte[] args = BitConverter.GetBytes((uint)deviceId)
            .Concat(parameters)
            .ToArray();

        byte[] result = CallMethodBufferWithRetry(method, args, logName ?? $"DeviceSet({deviceId})");
        _logger?.LogDebug("{Operation} = {Params} : {Result}", logName ?? $"DeviceSet({deviceId})",
            BitConverter.ToString(parameters), BitConverter.ToString(result));
        return result;
    }

    /// <summary>
    /// Writes a device value via WMI-based ACPI interface.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="status">The status value to write.</param>
    /// <returns>Status code (1=OK), or -1 on failure.</returns>
    public int DeviceSetWmi(AsusDevice deviceId, int status)
    {
        if (!IsAvailable) return -1;

        var method = DeviceMethod.DEVS;
        byte[] args = BitConverter.GetBytes((uint)deviceId)
            .Concat(BitConverter.GetBytes((uint)status))
            .ToArray();

        byte[] result = CallMethodBufferWithRetry(method, args, $"DeviceSetWmi({deviceId})");
        if (result.Length >= 4)
            return BitConverter.ToInt32(result, 0);
        return -1;
    }

    /// <summary>
    /// Reads a device value via ACPI.
    /// G-Helper protocol: returned value is Int32 - 65536.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <returns>The device value, or -1 if the operation failed.</returns>
    public int DeviceGet(AsusDevice deviceId)
    {
        if (!IsAvailable) return -1;

        var method = DeviceMethod.DSTS;
        byte[] args = BitConverter.GetBytes((uint)deviceId);

        byte[] buffer = CallMethodBufferWithRetry(method, args, $"DeviceGet({deviceId})");
        if (buffer.Length >= 4)
            return BitConverter.ToInt32(buffer, 0) - 65536;
        return -1;
    }

    /// <summary>
    /// Reads a device buffer via ACPI.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="status">The status parameter to include in the request.</param>
    /// <returns>The raw device buffer, or an empty array if the operation failed.</returns>
    public byte[] DeviceGetBuffer(AsusDevice deviceId, uint status)
    {
        if (!IsAvailable) return Array.Empty<byte>();

        var method = DeviceMethod.DSTS;
        byte[] args = BitConverter.GetBytes((uint)deviceId)
            .Concat(BitConverter.GetBytes(status))
            .ToArray();

        return CallMethodBufferWithRetry(method, args, $"DeviceGetBuffer({deviceId})");
    }

    /// <summary>
    /// Reads a large device buffer (256+ bytes) via ACPI with a configurable output size.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="extraIn">An extra input parameter to include in the request.</param>
    /// <param name="outSize">The size of the output buffer in bytes.</param>
    /// <returns>The device buffer, or an empty array if the operation failed.</returns>
    public byte[] DeviceGetLarge(AsusDevice deviceId, int extraIn, int outSize)
    {
        if (!IsAvailable) return Array.Empty<byte>();

        int bufferSize = Math.Max(outSize, 32);
        var method = DeviceMethod.DSTS;
        byte[] args = BitConverter.GetBytes((uint)deviceId)
            .Concat(BitConverter.GetBytes(extraIn))
            .ToArray();

        return CallMethodBufferWithRetry(method, args, $"DeviceGetLarge({deviceId})", bufferSize);
    }

    /// <summary>
    /// Initializes the ACPI device.
    /// </summary>
    /// <returns>True if initialization succeeded; otherwise false.</returns>
    public bool Initialize()
    {
        return CallMethodWithRetry(DeviceMethod.INIT, Array.Empty<byte>(), "Initialize");
    }

    /// <summary>
    /// Sets the watchdog timer.
    /// </summary>
    /// <param name="timeoutSeconds">The watchdog timeout in seconds.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetWatchdog(int timeoutSeconds)
    {
        return CallMethodWithRetry(DeviceMethod.WDOG, BitConverter.GetBytes(timeoutSeconds), $"SetWatchdog({timeoutSeconds}s)");
    }

    /// <summary>
    /// Sets the ASUS performance mode.
    /// </summary>
    /// <param name="mode">The performance mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetPerformanceMode(AsusMode mode)
    {
        return DeviceSet(AsusDevice.PerformanceMode, (int)mode, $"SetPerformanceMode({mode})") == 1;
    }

    /// <summary>
    /// Sets the ASUS status mode.
    /// </summary>
    /// <param name="status">The status mode value.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetStatusMode(int status)
    {
        return DeviceSet(AsusDevice.StatusMode, status, $"SetStatusMode({status})") == 1;
    }

    /// <summary>
    /// Gets the current ASUS performance mode.
    /// </summary>
    /// <returns>The performance mode value, or -1 if unavailable.</returns>
    public int GetPerformanceMode()
    {
        return DeviceGet(AsusDevice.PerformanceMode);
    }

    /// <summary>
    /// Sets the CPU fan curve.
    /// </summary>
    /// <param name="curve">The fan curve data bytes.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetCpuFanCurve(byte[] curve)
    {
        return DeviceSet(AsusDevice.DevsCPUFanCurve, curve, "SetCpuFanCurve").Length > 0;
    }

    /// <summary>
    /// Sets the GPU fan curve.
    /// </summary>
    /// <param name="curve">The fan curve data bytes.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGpuFanCurve(byte[] curve)
    {
        return DeviceSet(AsusDevice.DevsGPUFanCurve, curve, "SetGpuFanCurve").Length > 0;
    }

    /// <summary>
    /// Gets the current CPU temperature in degrees Celsius.
    /// Uses dedicated Temp_CPU sensor (0x00120094) not CPU_Fan.
    /// </summary>
    /// <returns>The temperature in Celsius, or -1 if unavailable.</returns>
    public int GetCpuTemperature()
    {
        return DeviceGet(AsusDevice.Temp_CPU);
    }

    /// <summary>
    /// Gets the current GPU temperature in degrees Celsius.
    /// Uses dedicated Temp_GPU sensor (0x00120097) not GPU_Fan.
    /// </summary>
    /// <returns>The temperature in Celsius, or -1 if unavailable.</returns>
    public int GetGpuTemperature()
    {
        return DeviceGet(AsusDevice.Temp_GPU);
    }

    /// <summary>
    /// Gets the battery discharge status via buffer reading.
    /// G-Helper protocol: reads BatteryDischarge buffer and parses charge + status.
    /// </summary>
    /// <returns>A tuple of (chargePercent, status) or (-1, -1) if unavailable.</returns>
    public (int charge, int status) GetBatteryDischarge()
    {
        if (!IsAvailable) return (-1, -1);

        try
        {
            byte[] buffer = DeviceGetBuffer(AsusDevice.BatteryDischarge, 0);

            if (buffer.Length < 4)
                return (-1, -1);

            int charge = BitConverter.ToInt16(buffer, 0);
            int status = buffer.Length > 2 ? buffer[2] : -1;

            return (charge, status);
        }
        catch
        {
            return (-1, -1);
        }
    }

    /// <summary>
    /// Sets the battery charge limit.
    /// </summary>
    /// <param name="limit">The charge limit percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetBatteryLimit(int limit)
    {
        return DeviceSet(AsusDevice.BatteryLimit, limit, $"SetBatteryLimit({limit}%)") == 1;
    }

    /// <summary>
    /// Gets the battery charge limit. This is a write-only device on most ASUS boards.
    /// G-Helper does not read it back — the limit is a user-configured value.
    /// Returns a sensible default (100) since the device does not support reading.
    /// </summary>
    public int GetBatteryLimit()
    {
        return 100;
    }

    /// <summary>
    /// Sets the keyboard backlight brightness level.
    /// </summary>
    /// <param name="level">The brightness level (typically 0-3).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetKeyboardBrightness(int level)
    {
        return DeviceSet(AsusDevice.KeyboardLight, level, $"SetKeyboardBrightness({level})") == 1;
    }

    /// <summary>
    /// Sets the GPU operating mode.
    /// </summary>
    /// <param name="mode">The GPU mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGPUMode(AsusGPU mode)
    {
        return DeviceSet(AsusDevice.GPUEco, (int)mode, $"SetGPUMode({mode})") == 1;
    }

    /// <summary>
    /// Gets the current GPU operating mode.
    /// </summary>
    /// <returns>The GPU mode value, or -1 if unavailable.</returns>
    public int GetGPUMode()
    {
        return DeviceGet(AsusDevice.GPUEco);
    }

    private static byte[] BuildArgs(AsusDevice deviceId, byte[] parameters)
    {
        return BitConverter.GetBytes((uint)deviceId)
            .Concat(parameters)
            .ToArray();
    }

    private byte[] BuildMethodBuffer(DeviceMethod method, byte[] args)
    {
        byte[] methodBytes = BitConverter.GetBytes((uint)method);
        byte[] argsLength = BitConverter.GetBytes(args.Length);
        return methodBytes.Concat(argsLength).Concat(args).ToArray();
    }

    private bool CallMethodWithRetry(DeviceMethod method, byte[] args, string operationName)
    {
        int retryDelay = BaseRetryDelayMs;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _logger?.LogDebug("Retry {Attempt}/{MaxRetries} for {Operation} after {Delay}ms",
                    attempt, MaxRetries, operationName, retryDelay);
                Thread.Sleep(retryDelay);
                retryDelay *= 2;
            }

            try
            {
                byte[] inBuffer = BuildMethodBuffer(method, args);
                bool result = _device.CallControl(inBuffer, DefaultBufferSize);

                if (result)
                {
                    if (attempt > 0)
                        _logger?.LogInformation("{Operation} succeeded on retry {Attempt}", operationName, attempt);
                    return true;
                }

                _logger?.LogWarning("{Operation} failed on attempt {Attempt}", operationName, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{Operation} threw exception on attempt {Attempt}", operationName, attempt + 1);
            }
        }

        _logger?.LogError("{Operation} failed after {MaxRetries} retries", operationName, MaxRetries);
        return false;
    }

    private byte[] CallMethodBufferWithRetry(DeviceMethod method, byte[] args, string operationName, int bufferSize = DefaultBufferSize)
    {
        int retryDelay = BaseRetryDelayMs;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _logger?.LogDebug("Retry {Attempt}/{MaxRetries} for {Operation} after {Delay}ms",
                    attempt, MaxRetries, operationName, retryDelay);
                Thread.Sleep(retryDelay);
                retryDelay *= 2;
            }

            try
            {
                byte[] inBuffer = BuildMethodBuffer(method, args);
                byte[] result = _device.CallControlBuffer(inBuffer, bufferSize);

                if (result.Length > 0)
                {
                    if (attempt > 0)
                        _logger?.LogInformation("{Operation} succeeded on retry {Attempt}", operationName, attempt);
                    return result;
                }

                _logger?.LogWarning("{Operation} returned empty buffer on attempt {Attempt}", operationName, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{Operation} threw exception on attempt {Attempt}", operationName, attempt + 1);
            }
        }

        _logger?.LogError("{Operation} failed after {MaxRetries} retries", operationName, MaxRetries);
        return Array.Empty<byte>();
    }

    /// <summary>
    /// Releases the unmanaged resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _device.Dispose();
    }
}