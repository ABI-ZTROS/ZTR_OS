using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Combines data from multiple sensor sources into a unified <see cref="HardwareState"/>.
/// Provides timestamp alignment, data normalization, and outlier detection.
/// </summary>
public class SensorAggregator
{
    private readonly double _outlierSigmaThreshold;
    private readonly int _alignmentWindowMs;

    /// <summary>
    /// Creates a new instance of the <see cref="SensorAggregator"/> class.
    /// </summary>
    /// <param name="outlierSigmaThreshold">
    /// The number of standard deviations beyond which a value is considered an outlier. Default is 3.0.
    /// </param>
    /// <param name="alignmentWindowMs">
    /// The maximum time window in milliseconds for sensor readings to be considered
    /// part of the same measurement batch. Default is 200ms.
    /// </param>
    public SensorAggregator(double outlierSigmaThreshold = 3.0, int alignmentWindowMs = 200)
    {
        _outlierSigmaThreshold = outlierSigmaThreshold;
        _alignmentWindowMs = alignmentWindowMs;
    }

    /// <summary>
    /// Aggregates a collection of sensor readings into a unified <see cref="HardwareState"/>.
    /// Groups readings by type, aligns timestamps within the configured window,
    /// normalizes values, and filters outliers.
    /// </summary>
    /// <param name="readings">The sensor readings to aggregate.</param>
    /// <returns>A <see cref="HardwareState"/> populated from the aggregated sensor data.</returns>
    public HardwareState Aggregate(IEnumerable<SensorReading> readings)
    {
        var readingList = readings?.ToList() ?? new List<SensorReading>();

        if (readingList.Count == 0)
            return new HardwareState { Timestamp = DateTime.UtcNow };

        var aligned = AlignTimestamps(readingList);
        var outliersFiltered = FilterOutliers(aligned);

        var state = new HardwareState
        {
            Timestamp = aligned.Select(r => r.Timestamp).DefaultIfEmpty(DateTime.UtcNow).Max(),
            Cpu = BuildCpuState(outliersFiltered),
            Gpu = BuildGpuState(outliersFiltered),
            Battery = BuildBatteryState(outliersFiltered),
            Fan = BuildFanState(outliersFiltered),
            Sensors = outliersFiltered.ToList().AsReadOnly()
        };

        return state;
    }

    /// <summary>
    /// Aligns sensor readings by grouping those within the configured time window.
    /// Readings that fall outside the window are normalized to the nearest group timestamp.
    /// </summary>
    /// <param name="readings">The readings to align.</param>
    /// <returns>The aligned readings with normalized timestamps.</returns>
    internal IReadOnlyList<SensorReading> AlignTimestamps(IReadOnlyList<SensorReading> readings)
    {
        if (readings.Count == 0)
            return Array.Empty<SensorReading>();

        var baseTime = readings.Min(r => r.Timestamp);
        var aligned = new List<SensorReading>(readings.Count);

        foreach (var reading in readings)
        {
            var offset = (reading.Timestamp - baseTime).TotalMilliseconds;
            if (Math.Abs(offset) <= _alignmentWindowMs)
            {
                aligned.Add(new SensorReading
                {
                    Name = reading.Name,
                    Value = reading.Value,
                    Unit = reading.Unit,
                    Type = reading.Type,
                    Timestamp = baseTime
                });
            }
            else
            {
                var normalizedTimestamp = baseTime.AddMilliseconds(Math.Round(offset / (double)_alignmentWindowMs) * _alignmentWindowMs);
                aligned.Add(new SensorReading
                {
                    Name = reading.Name,
                    Value = reading.Value,
                    Unit = reading.Unit,
                    Type = reading.Type,
                    Timestamp = normalizedTimestamp
                });
            }
        }

        return aligned.AsReadOnly();
    }

    /// <summary>
    /// Filters outlier readings by sensor name using standard deviation analysis.
    /// Readings that fall outside the configured sigma threshold are excluded.
    /// </summary>
    /// <param name="readings">The readings to filter.</param>
    /// <returns>The readings with outliers removed.</returns>
    internal IReadOnlyList<SensorReading> FilterOutliers(IReadOnlyList<SensorReading> readings)
    {
        if (readings.Count == 0)
            return Array.Empty<SensorReading>();

        var grouped = readings.GroupBy(r => r.Name);
        var result = new List<SensorReading>(readings.Count);

        foreach (var group in grouped)
        {
            var groupList = group.ToList();
            if (groupList.Count <= 1)
            {
                result.AddRange(groupList);
                continue;
            }

            var values = groupList.Select(r => r.Value).ToList();
            var mean = values.Average();
            var variance = values.Select(v => (v - mean) * (v - mean)).Sum() / values.Count;
            var stdDev = Math.Sqrt(variance);

            if (stdDev < double.Epsilon)
            {
                result.AddRange(groupList);
                continue;
            }

            foreach (var reading in groupList)
            {
                var zScore = Math.Abs(reading.Value - mean) / stdDev;
                if (zScore <= _outlierSigmaThreshold)
                {
                    result.Add(reading);
                }
            }
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Normalizes a value to a 0-1 range based on the provided min and max.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="min">The minimum value in the range.</param>
    /// <param name="max">The maximum value in the range.</param>
    /// <returns>The normalized value between 0 and 1.</returns>
    public static double Normalize(double value, double min, double max)
    {
        if (max <= min) return 0;
        return Math.Clamp((value - min) / (max - min), 0, 1);
    }

    /// <summary>
    /// Computes the moving average of a sequence of values.
    /// </summary>
    /// <param name="values">The values to average.</param>
    /// <returns>The average value, or 0 if the sequence is empty.</returns>
    public static double MovingAverage(IEnumerable<double> values)
    {
        var list = values?.ToList();
        if (list == null || list.Count == 0)
            return 0;
        return list.Average();
    }

    private static CpuState BuildCpuState(IEnumerable<SensorReading> readings)
    {
        var state = new CpuState();
        var cpuTempReadings = readings.Where(r => r.Type == SensorType.Temperature && r.Name == "CPU Temperature").ToList();
        if (cpuTempReadings.Count > 0)
            state.Temperature = (int)Math.Round(cpuTempReadings.Average(r => r.Value));

        var cpuUsageReadings = readings.Where(r => r.Type == SensorType.Usage && r.Name == "CPU Usage").ToList();
        if (cpuUsageReadings.Count > 0)
            state.Usage = (int)Math.Round(cpuUsageReadings.Average(r => r.Value));

        var cpuPowerReadings = readings.Where(r => r.Type == SensorType.Power && r.Name == "CPU Power").ToList();
        if (cpuPowerReadings.Count > 0)
            state.Power = (int)Math.Round(cpuPowerReadings.Average(r => r.Value));

        var cpuClockReadings = readings.Where(r => r.Type == SensorType.Clock && r.Name == "CPU Clock").ToList();
        if (cpuClockReadings.Count > 0)
            state.ClockMHz = (int)Math.Round(cpuClockReadings.Average(r => r.Value));

        var powerLimitReadings = readings.Where(r => r.Type == SensorType.Power && r.Name == "CPU PowerLimit").ToList();
        if (powerLimitReadings.Count > 0)
            state.PowerLimit = (int)Math.Round(powerLimitReadings.Average(r => r.Value));

        return state;
    }

    private static GpuState BuildGpuState(IEnumerable<SensorReading> readings)
    {
        var state = new GpuState();
        var gpuTempReadings = readings.Where(r => r.Type == SensorType.Temperature && r.Name.StartsWith("GPU", StringComparison.OrdinalIgnoreCase) && !r.Name.Contains("Hotspot", StringComparison.OrdinalIgnoreCase)).ToList();
        if (gpuTempReadings.Count > 0)
            state.Temperature = (int)Math.Round(gpuTempReadings.Average(r => r.Value));

        var hotspotReadings = readings.Where(r => r.Type == SensorType.Temperature && r.Name.Contains("Hotspot", StringComparison.OrdinalIgnoreCase)).ToList();
        if (hotspotReadings.Count > 0)
            state.HotspotTemperature = (int)Math.Round(hotspotReadings.Average(r => r.Value));

        var gpuUsageReadings = readings.Where(r => r.Type == SensorType.Usage && r.Name == "GPU Usage").ToList();
        if (gpuUsageReadings.Count > 0)
            state.Usage = (int)Math.Round(gpuUsageReadings.Average(r => r.Value));

        var gpuPowerReadings = readings.Where(r => r.Type == SensorType.Power && r.Name.StartsWith("GPU", StringComparison.OrdinalIgnoreCase)).ToList();
        if (gpuPowerReadings.Count > 0)
            state.Power = (int)Math.Round(gpuPowerReadings.Average(r => r.Value));

        var vramReadings = readings.Where(r => r.Name == "GPU VRAM").ToList();
        if (vramReadings.Count > 0)
            state.UsedVramMB = (long)Math.Round(vramReadings.Average(r => r.Value));

        var totalVramReadings = readings.Where(r => r.Name == "GPU TotalVRAM").ToList();
        if (totalVramReadings.Count > 0)
            state.TotalVramMB = (long)Math.Round(totalVramReadings.Average(r => r.Value));

        var coreClockReadings = readings.Where(r => r.Type == SensorType.Clock && r.Name == "GPU CoreClock").ToList();
        if (coreClockReadings.Count > 0)
            state.CoreClockMHz = (int)Math.Round(coreClockReadings.Average(r => r.Value));

        var memClockReadings = readings.Where(r => r.Type == SensorType.Clock && r.Name == "GPU MemoryClock").ToList();
        if (memClockReadings.Count > 0)
            state.MemoryClockMHz = (int)Math.Round(memClockReadings.Average(r => r.Value));

        return state;
    }

    private static BatteryState BuildBatteryState(IEnumerable<SensorReading> readings)
    {
        var state = new BatteryState();
        var chargeReadings = readings.Where(r => r.Name.Contains("BatteryCharge", StringComparison.OrdinalIgnoreCase) || r.Name.Contains("ChargePercent", StringComparison.OrdinalIgnoreCase)).ToList();
        var validChargeReadings = chargeReadings.Where(r => r.Value >= 0).ToList();
        if (validChargeReadings.Count > 0)
            state.ChargePercent = (int)Math.Round(validChargeReadings.Average(r => r.Value));

        var chargingReadings = readings.Where(r => r.Name.Contains("Charging", StringComparison.OrdinalIgnoreCase)).ToList();
        if (chargingReadings.Count > 0)
            state.IsCharging = chargingReadings.Average(r => r.Value) > 0.5;

        var chargeLimitReadings = readings.Where(r => r.Name.Contains("ChargeLimit", StringComparison.OrdinalIgnoreCase)).ToList();
        if (chargeLimitReadings.Count > 0)
            state.ChargeLimit = (int)Math.Round(chargeLimitReadings.Average(r => r.Value));

        var statusReadings = readings.Where(r => r.Name.Contains("BatteryStatus", StringComparison.OrdinalIgnoreCase)).ToList();
        if (statusReadings.Count > 0)
            state.Status = statusReadings.First().Value.ToString("F0");

        return state;
    }

    private static FanState BuildFanState(IEnumerable<SensorReading> readings)
    {
        var state = new FanState();
        var cpuFanSpeed = readings.Where(r => r.Type == SensorType.Fan && r.Name == "CPU Fan Speed" && r.Value >= 0).ToList();
        if (cpuFanSpeed.Count > 0)
            state.CpuFanSpeed = (int)Math.Round(cpuFanSpeed.Average(r => r.Value));

        var cpuFanRpm = readings.Where(r => r.Type == SensorType.Fan && r.Name == "CPU Fan RPM" && r.Value >= 0).ToList();
        if (cpuFanRpm.Count > 0)
            state.CpuFanRpm = (int)Math.Round(cpuFanRpm.Average(r => r.Value));

        var gpuFanSpeed = readings.Where(r => r.Type == SensorType.Fan && r.Name == "GPU Fan Speed" && r.Value >= 0).ToList();
        if (gpuFanSpeed.Count > 0)
            state.GpuFanSpeed = (int)Math.Round(gpuFanSpeed.Average(r => r.Value));

        var gpuFanRpm = readings.Where(r => r.Type == SensorType.Fan && r.Name == "GPU Fan RPM" && r.Value >= 0).ToList();
        if (gpuFanRpm.Count > 0)
            state.GpuFanRpm = (int)Math.Round(gpuFanRpm.Average(r => r.Value));

        var midFanSpeed = readings.Where(r => r.Type == SensorType.Fan && r.Name == "Mid Fan Speed" && r.Value >= 0).ToList();
        if (midFanSpeed.Count > 0)
            state.MidFanSpeed = (int)Math.Round(midFanSpeed.Average(r => r.Value));

        return state;
    }
}
