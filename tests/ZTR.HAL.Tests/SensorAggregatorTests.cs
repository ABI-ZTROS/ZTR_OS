using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class SensorAggregatorTests
{
    private readonly SensorAggregator _aggregator;

    public SensorAggregatorTests()
    {
        _aggregator = new SensorAggregator();
    }

    #region Aggregate Tests

    [Fact]
    public void Aggregate_EmptyReadings_ReturnsEmptyHardwareState()
    {
        var result = _aggregator.Aggregate(Array.Empty<SensorReading>());

        Assert.NotNull(result);
        Assert.NotNull(result.Cpu);
        Assert.NotNull(result.Gpu);
        Assert.NotNull(result.Battery);
        Assert.NotNull(result.Fan);
        Assert.True((DateTime.UtcNow - result.Timestamp).TotalSeconds < 5);
    }

    [Fact]
    public void Aggregate_NullReadings_ReturnsEmptyHardwareState()
    {
        var result = _aggregator.Aggregate(null!);

        Assert.NotNull(result);
    }

    [Fact]
    public void Aggregate_CpuTemperature_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Unit = "°C", Type = SensorType.Temperature, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(45, result.Cpu.Temperature);
    }

    [Fact]
    public void Aggregate_CpuPower_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Power", Value = 65, Unit = "W", Type = SensorType.Power, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(65, result.Cpu.Power);
    }

    [Fact]
    public void Aggregate_CpuClock_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Clock", Value = 3200, Unit = "MHz", Type = SensorType.Clock, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(3200, result.Cpu.ClockMHz);
    }

    [Fact]
    public void Aggregate_CpuPowerLimit_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU PowerLimit", Value = 105, Unit = "W", Type = SensorType.Power, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(105, result.Cpu.PowerLimit);
    }

    [Fact]
    public void Aggregate_GpuTemperature_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Temperature", Value = 62, Unit = "°C", Type = SensorType.Temperature, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(62, result.Gpu.Temperature);
    }

    [Fact]
    public void Aggregate_GpuHotspot_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Hotspot Temperature", Value = 78, Unit = "°C", Type = SensorType.Temperature, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(78, result.Gpu.HotspotTemperature);
    }

    [Fact]
    public void Aggregate_GpuUsage_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Usage", Value = 75, Unit = "%", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(75, result.Gpu.Usage);
    }

    [Fact]
    public void Aggregate_GpuPower_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Power", Value = 180, Unit = "W", Type = SensorType.Power, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(180, result.Gpu.Power);
    }

    [Fact]
    public void Aggregate_GpuCoreClock_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU CoreClock", Value = 1800, Unit = "MHz", Type = SensorType.Clock, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(1800, result.Gpu.CoreClockMHz);
    }

    [Fact]
    public void Aggregate_GpuMemoryClock_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU MemoryClock", Value = 5000, Unit = "MHz", Type = SensorType.Clock, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(5000, result.Gpu.MemoryClockMHz);
    }

    [Fact]
    public void Aggregate_GpuVram_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU VRAM", Value = 6144, Unit = "MB", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(6144, result.Gpu.UsedVramMB);
    }

    [Fact]
    public void Aggregate_GpuTotalVram_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU TotalVRAM", Value = 8192, Unit = "MB", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(8192, result.Gpu.TotalVramMB);
    }

    [Fact]
    public void Aggregate_BatteryCharge_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "BatteryCharge", Value = 75, Unit = "%", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(75, result.Battery.ChargePercent);
    }

    [Fact]
    public void Aggregate_Charging_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "Charging", Value = 1, Unit = "bool", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.True(result.Battery.IsCharging);
    }

    [Fact]
    public void Aggregate_ChargeLimit_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "ChargeLimit", Value = 80, Unit = "%", Type = SensorType.Usage, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(80, result.Battery.ChargeLimit);
    }

    [Fact]
    public void Aggregate_CpuFanRpm_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Fan RPM", Value = 2400, Unit = "RPM", Type = SensorType.Fan, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(2400, result.Fan.CpuFanRpm);
    }

    [Fact]
    public void Aggregate_GpuFanRpm_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Fan RPM", Value = 3200, Unit = "RPM", Type = SensorType.Fan, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(3200, result.Fan.GpuFanRpm);
    }

    [Fact]
    public void Aggregate_GpuFanSpeed_Readings_ParsedCorrectly()
    {
        var readings = new[]
        {
            new SensorReading { Name = "GPU Fan Speed", Value = 65, Unit = "%", Type = SensorType.Fan, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(65, result.Fan.GpuFanSpeed);
    }

    [Fact]
    public void Aggregate_MultipleReadings_AllParsed()
    {
        var now = DateTime.UtcNow;
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Unit = "°C", Type = SensorType.Temperature, Timestamp = now },
            new SensorReading { Name = "CPU Power", Value = 65, Unit = "W", Type = SensorType.Power, Timestamp = now },
            new SensorReading { Name = "GPU Temperature", Value = 62, Unit = "°C", Type = SensorType.Temperature, Timestamp = now },
            new SensorReading { Name = "GPU Usage", Value = 75, Unit = "%", Type = SensorType.Usage, Timestamp = now },
            new SensorReading { Name = "BatteryCharge", Value = 80, Unit = "%", Type = SensorType.Usage, Timestamp = now },
            new SensorReading { Name = "CPU Fan RPM", Value = 2400, Unit = "RPM", Type = SensorType.Fan, Timestamp = now },
            new SensorReading { Name = "GPU Fan RPM", Value = 3200, Unit = "RPM", Type = SensorType.Fan, Timestamp = now }
        };

        var result = _aggregator.Aggregate(readings);

        Assert.Equal(45, result.Cpu.Temperature);
        Assert.Equal(65, result.Cpu.Power);
        Assert.Equal(62, result.Gpu.Temperature);
        Assert.Equal(75, result.Gpu.Usage);
        Assert.Equal(80, result.Battery.ChargePercent);
        Assert.Equal(2400, result.Fan.CpuFanRpm);
        Assert.Equal(3200, result.Fan.GpuFanRpm);
        Assert.NotEmpty(result.Sensors);
    }

    #endregion

    #region Timestamp Alignment Tests

    [Fact]
    public void AlignTimestamps_ReadingsWithinWindow_AlignedToBase()
    {
        var now = DateTime.UtcNow;
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = now },
            new SensorReading { Name = "GPU Temperature", Value = 60, Timestamp = now.AddMilliseconds(100) }
        };

        var result = _aggregator.AlignTimestamps(readings);

        Assert.Equal(2, result.Count);
        Assert.Equal(result[0].Timestamp, result[1].Timestamp);
    }

    [Fact]
    public void AlignTimestamps_ReadingsOutsideWindow_Normalized()
    {
        var now = DateTime.UtcNow;
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = now },
            new SensorReading { Name = "GPU Temperature", Value = 60, Timestamp = now.AddMilliseconds(500) }
        };

        var result = _aggregator.AlignTimestamps(readings);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void AlignTimestamps_EmptyList_ReturnsEmpty()
    {
        var result = _aggregator.AlignTimestamps(Array.Empty<SensorReading>());

        Assert.Empty(result);
    }

    [Fact]
    public void AlignTimestamps_SingleReading_ReturnsSingle()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.AlignTimestamps(readings);

        Assert.Single(result);
    }

    #endregion

    #region Outlier Detection Tests

    [Fact]
    public void FilterOutliers_NoOutliers_ReturnsAll()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 46, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 47, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.FilterOutliers(readings);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterOutliers_WithOutlier_RemovesOutlier()
    {
        var agg = new SensorAggregator(outlierSigmaThreshold: 2.0);
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 46, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 47, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 48, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 49, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 200, Timestamp = DateTime.UtcNow }
        };

        var result = agg.FilterOutliers(readings);

        Assert.Equal(5, result.Count);
        Assert.DoesNotContain(result, r => r.Value == 200);
    }

    [Fact]
    public void FilterOutliers_SingleReading_ReturnsAll()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.FilterOutliers(readings);

        Assert.Single(result);
    }

    [Fact]
    public void FilterOutliers_EmptyList_ReturnsEmpty()
    {
        var result = _aggregator.FilterOutliers(Array.Empty<SensorReading>());

        Assert.Empty(result);
    }

    [Fact]
    public void FilterOutliers_ZeroStdDev_ReturnsAll()
    {
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow }
        };

        var result = _aggregator.FilterOutliers(readings);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterOutliers_MixedSensors_GroupedIndependently()
    {
        var agg = new SensorAggregator(outlierSigmaThreshold: 2.0);
        var readings = new[]
        {
            new SensorReading { Name = "CPU Temperature", Value = 45, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 46, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 47, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 48, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 49, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "CPU Temperature", Value = 200, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "GPU Temperature", Value = 60, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "GPU Temperature", Value = 61, Timestamp = DateTime.UtcNow },
            new SensorReading { Name = "GPU Temperature", Value = 62, Timestamp = DateTime.UtcNow }
        };

        var result = agg.FilterOutliers(readings);

        Assert.Equal(8, result.Count);
    }

    #endregion

    #region Normalize Tests

    [Fact]
    public void Normalize_ValueInRange_ReturnsCorrectRatio()
    {
        double result = SensorAggregator.Normalize(50, 0, 100);
        Assert.Equal(0.5, result, 6);
    }

    [Fact]
    public void Normalize_ValueAtMin_ReturnsZero()
    {
        double result = SensorAggregator.Normalize(0, 0, 100);
        Assert.Equal(0, result, 6);
    }

    [Fact]
    public void Normalize_ValueAtMax_ReturnsOne()
    {
        double result = SensorAggregator.Normalize(100, 0, 100);
        Assert.Equal(1, result, 6);
    }

    [Fact]
    public void Normalize_ValueBelowMin_ClampedToZero()
    {
        double result = SensorAggregator.Normalize(-10, 0, 100);
        Assert.Equal(0, result, 6);
    }

    [Fact]
    public void Normalize_ValueAboveMax_ClampedToOne()
    {
        double result = SensorAggregator.Normalize(150, 0, 100);
        Assert.Equal(1, result, 6);
    }

    [Fact]
    public void Normalize_MinEqualsMax_ReturnsZero()
    {
        double result = SensorAggregator.Normalize(50, 50, 50);
        Assert.Equal(0, result, 6);
    }

    #endregion

    #region MovingAverage Tests

    [Fact]
    public void MovingAverage_MultipleValues_ReturnsAverage()
    {
        double result = SensorAggregator.MovingAverage(new[] { 10.0, 20.0, 30.0 });
        Assert.Equal(20, result, 6);
    }

    [Fact]
    public void MovingAverage_SingleValue_ReturnsValue()
    {
        double result = SensorAggregator.MovingAverage(new[] { 42.0 });
        Assert.Equal(42, result, 6);
    }

    [Fact]
    public void MovingAverage_Empty_ReturnsZero()
    {
        double result = SensorAggregator.MovingAverage(Array.Empty<double>());
        Assert.Equal(0, result, 6);
    }

    [Fact]
    public void MovingAverage_Null_ReturnsZero()
    {
        double result = SensorAggregator.MovingAverage(null!);
        Assert.Equal(0, result, 6);
    }

    #endregion

    #region Aggregator Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        var agg = new SensorAggregator();
        Assert.NotNull(agg);
    }

    [Fact]
    public void Constructor_CustomThreshold_CreatesInstance()
    {
        var agg = new SensorAggregator(2.5, 100);
        Assert.NotNull(agg);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void FullPipeline_SimulatedReadings_AggregatedCorrectly()
    {
        var now = DateTime.UtcNow;
        var random = new Random(42);
        var readings = new List<SensorReading>();

        for (int i = 0; i < 10; i++)
        {
            readings.Add(new SensorReading { Name = "CPU Temperature", Value = 40 + random.NextDouble() * 10, Unit = "°C", Type = SensorType.Temperature, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "CPU Power", Value = 60 + random.NextDouble() * 20, Unit = "W", Type = SensorType.Power, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "GPU Temperature", Value = 55 + random.NextDouble() * 15, Unit = "°C", Type = SensorType.Temperature, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "GPU Usage", Value = 50 + random.NextDouble() * 40, Unit = "%", Type = SensorType.Usage, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "BatteryCharge", Value = 50 + random.NextDouble() * 40, Unit = "%", Type = SensorType.Usage, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "CPU Fan RPM", Value = 2000 + random.Next(500), Unit = "RPM", Type = SensorType.Fan, Timestamp = now.AddMilliseconds(i * 50) });
            readings.Add(new SensorReading { Name = "GPU Fan RPM", Value = 2500 + random.Next(500), Unit = "RPM", Type = SensorType.Fan, Timestamp = now.AddMilliseconds(i * 50) });
        }

        var result = _aggregator.Aggregate(readings);

        Assert.InRange(result.Cpu.Temperature, 30, 60);
        Assert.InRange(result.Cpu.Power, 40, 90);
        Assert.InRange(result.Gpu.Temperature, 40, 80);
        Assert.InRange(result.Gpu.Usage, 30, 100);
        Assert.InRange(result.Battery.ChargePercent, 30, 100);
        Assert.InRange(result.Fan.CpuFanRpm, 1500, 3000);
        Assert.InRange(result.Fan.GpuFanRpm, 2000, 3500);
    }

    #endregion
}
