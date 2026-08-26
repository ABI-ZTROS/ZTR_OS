using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

public class SensorPipeline : IDisposable
{
    private readonly AsusAcpi? _acpi;
    private readonly IGpuControl? _gpuControl;
    private readonly BatteryControl? _batteryControl;
    private readonly SensorAggregator _aggregator;
    private readonly SensorQueue _queue;
    private readonly SensorDegradationHandler _degradationHandler;
    private readonly ISystemSensorFallback? _systemFallback;
    private readonly ILogger<SensorPipeline>? _logger;
    private readonly Timer _timer;
    private int _intervalMs = 1000;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// Valid range is 100ms to 5000ms. Default is 1000ms.
    /// Changing this value restarts the collection timer.
    /// </summary>
    public int IntervalMs
    {
        get => _intervalMs;
        set
        {
            var clamped = Math.Clamp(value, 100, 5000);
            if (clamped != _intervalMs)
            {
                _intervalMs = clamped;
                RestartTimer();
            }
        }
    }

    /// <summary>
    /// Gets the sensor queue used for storing collected hardware states.
    /// </summary>
    public SensorQueue Queue => _queue;

    /// <summary>
    /// Gets the degradation handler used for sensor health tracking.
    /// </summary>
    public SensorDegradationHandler DegradationHandler => _degradationHandler;

    /// <summary>
    /// Gets the sensor aggregator used for combining readings.
    /// </summary>
    public SensorAggregator Aggregator => _aggregator;

    public SensorPipeline(
        AsusAcpi? acpi = null,
        IGpuControl? gpuControl = null,
        BatteryControl? batteryControl = null,
        SensorAggregator? aggregator = null,
        SensorQueue? queue = null,
        SensorDegradationHandler? degradationHandler = null,
        ISystemSensorFallback? systemFallback = null,
        ILogger<SensorPipeline>? logger = null)
    {
        _acpi = acpi;
        _gpuControl = gpuControl;
        _batteryControl = batteryControl;
        _aggregator = aggregator ?? new SensorAggregator();
        _queue = queue ?? new SensorQueue(1000);
        _degradationHandler = degradationHandler ?? new SensorDegradationHandler();
        _systemFallback = systemFallback;
        _logger = logger;

        if (_systemFallback == null || !_systemFallback.IsAvailable)
        {
            _logger?.LogWarning("SystemSensorFallback is not available. Some sensors may show as 0.");
        }
        else
        {
            _logger?.LogInformation("SystemSensorFallback is available for basic sensor data.");
        }

        RegisterSensors();

        _timer = new Timer(_ => CollectData(), null, Timeout.Infinite, _intervalMs);
    }

    /// <summary>
    /// Starts the sensor data collection.
    /// </summary>
    public void Start() => _timer.Change(0, _intervalMs);

    /// <summary>
    /// Stops the sensor data collection.
    /// </summary>
    public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

    /// <summary>
    /// Gets the latest hardware states from the queue.
    /// </summary>
    /// <param name="count">The maximum number of states to retrieve.</param>
    /// <returns>A read-only list of recent hardware states.</returns>
    public IReadOnlyList<HardwareState> GetLatestStates(int count = 100)
    {
        return _queue.GetHistory(count);
    }

    /// <summary>
    /// Performs a single data collection pass and returns the aggregated hardware state.
    /// This is useful for on-demand sampling outside the timer-based loop.
    /// </summary>
    /// <returns>The collected hardware state.</returns>
    public virtual HardwareState CollectOnce()
    {
        var state = CollectDataCore();
        _queue.Enqueue(state);
        return state;
    }

    private void RegisterSensors()
    {
        _degradationHandler.RegisterSensor("CPU Temperature", 0, 110);
        _degradationHandler.RegisterSensor("CPU Usage", 0, 100);
        _degradationHandler.RegisterSensor("CPU Power", 0, 500);
        _degradationHandler.RegisterSensor("CPU Clock", 0, 10000);
        _degradationHandler.RegisterSensor("CPU PowerLimit", 0, 500);
        _degradationHandler.RegisterSensor("GPU Temperature", 0, 110);
        _degradationHandler.RegisterSensor("GPU Hotspot Temperature", 0, 120);
        _degradationHandler.RegisterSensor("GPU Usage", 0, 100);
        _degradationHandler.RegisterSensor("GPU Power", 0, 1000);
        _degradationHandler.RegisterSensor("GPU CoreClock", 0, 5000);
        _degradationHandler.RegisterSensor("GPU MemoryClock", 0, 5000);
        _degradationHandler.RegisterSensor("GPU VRAM", 0, 32768);
        _degradationHandler.RegisterSensor("GPU TotalVRAM", 0, 32768);
        _degradationHandler.RegisterSensor("BatteryCharge", 0, 100);
        _degradationHandler.RegisterSensor("Charging", 0, 1);
        _degradationHandler.RegisterSensor("ChargeLimit", 0, 100);
        _degradationHandler.RegisterSensor("CPU Fan Speed", 0, 100);
        _degradationHandler.RegisterSensor("CPU Fan RPM", 0, 5000);
        _degradationHandler.RegisterSensor("GPU Fan Speed", 0, 100);
        _degradationHandler.RegisterSensor("GPU Fan RPM", 0, 5000);
        _degradationHandler.RegisterSensor("Mid Fan Speed", 0, 100);
    }

    private void CollectData()
    {
        try
        {
            var state = CollectDataCore();
            _queue.Enqueue(state);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during sensor data collection");
        }
    }

    private HardwareState CollectDataCore()
    {
        var timestamp = DateTime.UtcNow;
        var readings = new List<SensorReading>();

        readings.AddRange(CollectCpuReadings(timestamp));
        readings.AddRange(CollectGpuReadings(timestamp));
        readings.AddRange(CollectBatteryReadings(timestamp));
        readings.AddRange(CollectFanReadings(timestamp));

        var state = _aggregator.Aggregate(readings);
        state.Timestamp = timestamp;

        return state;
    }

    private IEnumerable<SensorReading> CollectCpuReadings(DateTime timestamp)
    {
        var readings = new List<SensorReading>();
        bool useFallback = _acpi == null || !_acpi.IsAvailable;

        int cpuTemp = SafeDeviceGet(AsusDevice.CPU_Fan);
        if (cpuTemp <= 0 && _systemFallback?.IsAvailable == true)
        {
            cpuTemp = _systemFallback.GetCpuTemperature();
        }

        if (cpuTemp > 0 && _degradationHandler.IsValueInRange("CPU Temperature", cpuTemp))
        {
            readings.Add(new SensorReading
            {
                Name = "CPU Temperature",
                Value = cpuTemp,
                Unit = "°C",
                Type = SensorType.Temperature,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU Temperature", cpuTemp, timestamp);
        }
        else
        {
            _degradationHandler.ReportFailure("CPU Temperature", "Out of range");
        }

        int cpuUsage = 0;
        if (_systemFallback?.IsAvailable == true)
        {
            cpuUsage = _systemFallback.GetCpuUsage();
        }
        else
        {
            cpuUsage = SafeDeviceGet(AsusDevice.CPU_Fan);
        }

        if (cpuUsage > 0)
        {
            readings.Add(new SensorReading
            {
                Name = "CPU Usage",
                Value = cpuUsage,
                Unit = "%",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU Usage", cpuUsage, timestamp);
        }

        int cpuPower = SafeDeviceGet(AsusDevice.PPT_APUA0);
        if (cpuPower <= 0 && _systemFallback?.IsAvailable == true)
        {
            cpuPower = _systemFallback.GetCpuPower();
        }

        if (cpuPower > 0)
        {
            readings.Add(new SensorReading
            {
                Name = "CPU Power",
                Value = cpuPower,
                Unit = "W",
                Type = SensorType.Power,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU Power", cpuPower, timestamp);
        }

        int cpuPowerLimit = SafeDeviceGet(AsusDevice.PPT_APUC1);
        if (cpuPowerLimit > 0)
        {
            readings.Add(new SensorReading
            {
                Name = "CPU PowerLimit",
                Value = cpuPowerLimit,
                Unit = "W",
                Type = SensorType.Power,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU PowerLimit", cpuPowerLimit, timestamp);
        }

        int cpuClock = SafeDeviceGet(AsusDevice.GPUBase);
        if (cpuClock > 0)
        {
            readings.Add(new SensorReading
            {
                Name = "CPU Clock",
                Value = cpuClock,
                Unit = "MHz",
                Type = SensorType.Clock,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU Clock", cpuClock, timestamp);
        }

        return readings;
    }

    private IEnumerable<SensorReading> CollectGpuReadings(DateTime timestamp)
    {
        var readings = new List<SensorReading>();

        if (_gpuControl != null)
        {
            var gpuTemp = _gpuControl.GetCurrentTemperature();
            if (gpuTemp.HasValue && gpuTemp.Value > 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Temperature",
                    Value = gpuTemp.Value,
                    Unit = "°C",
                    Type = SensorType.Temperature,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Temperature", gpuTemp.Value, timestamp);
            }
            else if (_systemFallback?.IsAvailable == true)
            {
                int fallbackTemp = _systemFallback.GetGpuTemperature();
                if (fallbackTemp > 0)
                {
                    readings.Add(new SensorReading
                    {
                        Name = "GPU Temperature",
                        Value = fallbackTemp,
                        Unit = "°C",
                        Type = SensorType.Temperature,
                        Timestamp = timestamp
                    });
                    _degradationHandler.ReportSuccess("GPU Temperature", fallbackTemp, timestamp);
                }
            }

            var hotspotTemp = _gpuControl.GetHotspotTemperature();
            if (hotspotTemp.HasValue && hotspotTemp.Value > 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Hotspot Temperature",
                    Value = hotspotTemp.Value,
                    Unit = "°C",
                    Type = SensorType.Temperature,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Hotspot Temperature", hotspotTemp.Value, timestamp);
            }

            var gpuUsage = _gpuControl.GetGpuUse();
            if (gpuUsage.HasValue)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Usage",
                    Value = gpuUsage.Value,
                    Unit = "%",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Usage", gpuUsage.Value, timestamp);
            }
            else if (_systemFallback?.IsAvailable == true)
            {
                int fallbackUsage = _systemFallback.GetGpuUsage();
                readings.Add(new SensorReading
                {
                    Name = "GPU Usage",
                    Value = fallbackUsage,
                    Unit = "%",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Usage", fallbackUsage, timestamp);
            }

            var gpuPower = _gpuControl.GetGpuPower();
            if (gpuPower.HasValue)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Power",
                    Value = gpuPower.Value,
                    Unit = "W",
                    Type = SensorType.Power,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Power", gpuPower.Value, timestamp);
            }

            var clocks = _gpuControl.GetClockInfo();
            if (clocks.HasValue)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU CoreClock",
                    Value = clocks.Value.coreClockMHz,
                    Unit = "MHz",
                    Type = SensorType.Clock,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU CoreClock", clocks.Value.coreClockMHz, timestamp);

                readings.Add(new SensorReading
                {
                    Name = "GPU MemoryClock",
                    Value = clocks.Value.memoryClockMHz,
                    Unit = "MHz",
                    Type = SensorType.Clock,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU MemoryClock", clocks.Value.memoryClockMHz, timestamp);
            }

            var vram = _gpuControl.GetVramInfo();
            if (vram.HasValue)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU VRAM",
                    Value = vram.Value.usedMb,
                    Unit = "MB",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU VRAM", vram.Value.usedMb, timestamp);

                readings.Add(new SensorReading
                {
                    Name = "GPU TotalVRAM",
                    Value = vram.Value.totalMb,
                    Unit = "MB",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU TotalVRAM", vram.Value.totalMb, timestamp);
            }
        }
        else if (_systemFallback?.IsAvailable == true)
        {
            int gpuTemp = _systemFallback.GetGpuTemperature();
            if (gpuTemp > 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Temperature",
                    Value = gpuTemp,
                    Unit = "°C",
                    Type = SensorType.Temperature,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Temperature", gpuTemp, timestamp);
            }

            int gpuUsage = _systemFallback.GetGpuUsage();
            readings.Add(new SensorReading
            {
                Name = "GPU Usage",
                Value = gpuUsage,
                Unit = "%",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("GPU Usage", gpuUsage, timestamp);
        }

        return readings;
    }

    private IEnumerable<SensorReading> CollectBatteryReadings(DateTime timestamp)
    {
        var readings = new List<SensorReading>();
        BatteryInfo? info = null;

        try
        {
            if (_batteryControl != null)
            {
                info = _batteryControl.GetBatteryInfo();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "BatteryControl failed, falling back to system sensor");
        }

        bool hasValidBatteryData = false;

        if (info != null && info.ChargePercent >= 0)
        {
            readings.Add(new SensorReading
            {
                Name = "BatteryCharge",
                Value = info.ChargePercent,
                Unit = "%",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("BatteryCharge", info.ChargePercent, timestamp);

            readings.Add(new SensorReading
            {
                Name = "Charging",
                Value = info.IsCharging ? 1 : 0,
                Unit = "bool",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("Charging", info.IsCharging ? 1 : 0, timestamp);

            readings.Add(new SensorReading
            {
                Name = "ChargeLimit",
                Value = info.ChargeLimit,
                Unit = "%",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("ChargeLimit", info.ChargeLimit, timestamp);
            hasValidBatteryData = true;
        }

        if (!hasValidBatteryData && _systemFallback?.IsAvailable == true)
        {
            var batteryState = _systemFallback.GetBatteryState();

            if (batteryState.ChargePercent >= 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "BatteryCharge",
                    Value = batteryState.ChargePercent,
                    Unit = "%",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("BatteryCharge", batteryState.ChargePercent, timestamp);
                hasValidBatteryData = true;
            }

            readings.Add(new SensorReading
            {
                Name = "Charging",
                Value = batteryState.IsCharging ? 1 : 0,
                Unit = "bool",
                Type = SensorType.Usage,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("Charging", batteryState.IsCharging ? 1 : 0, timestamp);

            if (batteryState.ChargeLimit > 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "ChargeLimit",
                    Value = batteryState.ChargeLimit,
                    Unit = "%",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("ChargeLimit", batteryState.ChargeLimit, timestamp);
            }
        }

        if (!hasValidBatteryData)
        {
            int chargeLimit = SafeDeviceGet(AsusDevice.BatteryLimit);
            if (chargeLimit >= 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "ChargeLimit",
                    Value = chargeLimit,
                    Unit = "%",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("ChargeLimit", chargeLimit, timestamp);
            }

            int chargerMode = SafeDeviceGet(AsusDevice.ChargerMode);
            if (chargerMode >= 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "Charging",
                    Value = chargerMode != (int)ChargerMode.BatteryOnly ? 1 : 0,
                    Unit = "bool",
                    Type = SensorType.Usage,
                    Timestamp = timestamp
                });
            }
        }

        return readings;
    }

    private IEnumerable<SensorReading> CollectFanReadings(DateTime timestamp)
    {
        var readings = new List<SensorReading>();

        int cpuFanRpm = SafeDeviceGet(AsusDevice.CPU_Fan);
        if (cpuFanRpm >= 0)
        {
            readings.Add(new SensorReading
            {
                Name = "CPU Fan RPM",
                Value = cpuFanRpm,
                Unit = "RPM",
                Type = SensorType.Fan,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("CPU Fan RPM", cpuFanRpm, timestamp);
        }

        int gpuFanRpm = SafeDeviceGet(AsusDevice.GPU_Fan);
        if (gpuFanRpm >= 0)
        {
            readings.Add(new SensorReading
            {
                Name = "GPU Fan RPM",
                Value = gpuFanRpm,
                Unit = "RPM",
                Type = SensorType.Fan,
                Timestamp = timestamp
            });
            _degradationHandler.ReportSuccess("GPU Fan RPM", gpuFanRpm, timestamp);
        }

        int midFanRpm = SafeDeviceGet(AsusDevice.Mid_Fan);
        if (midFanRpm >= 0)
        {
            readings.Add(new SensorReading
            {
                Name = "Mid Fan Speed",
                Value = midFanRpm,
                Unit = "RPM",
                Type = SensorType.Fan,
                Timestamp = timestamp
            });
        }

        if (_gpuControl != null)
        {
            var gpuFanSpeed = _gpuControl.GetFanSpeed();
            if (gpuFanSpeed.HasValue && gpuFanSpeed.Value >= 0)
            {
                readings.Add(new SensorReading
                {
                    Name = "GPU Fan Speed",
                    Value = gpuFanSpeed.Value,
                    Unit = "%",
                    Type = SensorType.Fan,
                    Timestamp = timestamp
                });
                _degradationHandler.ReportSuccess("GPU Fan Speed", gpuFanSpeed.Value, timestamp);
            }
        }

        return readings;
    }

    private int SafeDeviceGet(AsusDevice device)
    {
        try
        {
            return _acpi?.DeviceGet(device) ?? -1;
        }
        catch
        {
            return -1;
        }
    }

    private void RestartTimer()
    {
        if (!_disposed)
        {
            _timer.Change(0, _intervalMs);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _timer.Dispose();
            _queue.Dispose();
            _disposed = true;
        }
    }
}
