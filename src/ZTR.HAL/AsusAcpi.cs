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
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="status">The status value to write.</param>
    /// <param name="logName">Optional descriptive name for logging.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool DeviceSet(AsusDevice deviceId, int status, string? logName = null)
    {
        if (!IsAvailable) return false;

        var method = DeviceMethod.DEVS;
        byte[] args = BuildArgs(deviceId, BitConverter.GetBytes(status));

        return CallMethodWithRetry(method, args, logName ?? $"DeviceSet({deviceId})");
    }

    /// <summary>
    /// Writes device bytes via ACPI.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="parameters">The raw parameter bytes to write.</param>
    /// <param name="logName">Optional descriptive name for logging.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool DeviceSet(AsusDevice deviceId, byte[] parameters, string? logName = null)
    {
        if (!IsAvailable) return false;

        var method = DeviceMethod.DEVS;
        byte[] args = BuildArgs(deviceId, parameters);

        return CallMethodWithRetry(method, args, logName ?? $"DeviceSet({deviceId})");
    }

    /// <summary>
    /// Writes a device value via WMI-based ACPI interface.
    /// </summary>
    /// <param name="deviceId">The ASUS device identifier.</param>
    /// <param name="status">The status value to write.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool DeviceSetWmi(AsusDevice deviceId, int status)
    {
        if (!IsAvailable) return false;

        var method = DeviceMethod.DEVS;
        byte[] args = BuildArgs(deviceId, BitConverter.GetBytes(status));

        return CallMethodWithRetry(method, args, $"DeviceSetWmi({deviceId})");
    }

    /// <summary>
    /// Reads a device value via ACPI.
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
            return BitConverter.ToInt32(buffer, 0);
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
        return DeviceSet(AsusDevice.PerformanceMode, (int)mode, $"SetPerformanceMode({mode})");
    }

    /// <summary>
    /// Sets the ASUS status mode.
    /// </summary>
    /// <param name="status">The status mode value.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetStatusMode(int status)
    {
        return DeviceSet(AsusDevice.StatusMode, status, $"SetStatusMode({status})");
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
        return DeviceSet(AsusDevice.DevsCPUFanCurve, curve, "SetCpuFanCurve");
    }

    /// <summary>
    /// Sets the GPU fan curve.
    /// </summary>
    /// <param name="curve">The fan curve data bytes.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGpuFanCurve(byte[] curve)
    {
        return DeviceSet(AsusDevice.DevsGPUFanCurve, curve, "SetGpuFanCurve");
    }

    /// <summary>
    /// Gets the current CPU temperature in degrees Celsius.
    /// </summary>
    /// <returns>The temperature in Celsius, or -1 if unavailable.</returns>
    public int GetCpuTemperature()
    {
        return DeviceGet(AsusDevice.CPU_Fan);
    }

    /// <summary>
    /// Gets the current GPU temperature in degrees Celsius.
    /// </summary>
    /// <returns>The temperature in Celsius, or -1 if unavailable.</returns>
    public int GetGpuTemperature()
    {
        return DeviceGet(AsusDevice.GPU_Fan);
    }

    /// <summary>
    /// Sets the battery charge limit.
    /// </summary>
    /// <param name="limit">The charge limit percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetBatteryLimit(int limit)
    {
        return DeviceSet(AsusDevice.BatteryLimit, limit, $"SetBatteryLimit({limit}%)");
    }

    /// <summary>
    /// Gets the current battery charge limit.
    /// </summary>
    /// <returns>The charge limit percentage, or -1 if unavailable.</returns>
    public int GetBatteryLimit()
    {
        return DeviceGet(AsusDevice.BatteryLimit);
    }

    /// <summary>
    /// Sets the keyboard backlight brightness level.
    /// </summary>
    /// <param name="level">The brightness level (typically 0-3).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetKeyboardBrightness(int level)
    {
        return DeviceSet(AsusDevice.KeyboardLight, level, $"SetKeyboardBrightness({level})");
    }

    /// <summary>
    /// Sets the GPU operating mode.
    /// </summary>
    /// <param name="mode">The GPU mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGPUMode(AsusGPU mode)
    {
        return DeviceSet(AsusDevice.GPUEco, (int)mode, $"SetGPUMode({mode})");
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