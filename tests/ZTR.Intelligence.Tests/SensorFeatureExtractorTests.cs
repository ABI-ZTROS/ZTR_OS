using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class SensorFeatureExtractorTests
{
    private readonly SensorFeatureExtractor _extractor;

    public SensorFeatureExtractorTests()
    {
        _extractor = new SensorFeatureExtractor(windowSeconds: 5, featureCount: 20);
    }

    [Fact]
    public void ExtractFeatures_WithValidState_ReturnsCorrectFeatureCount()
    {
        var current = CreateTestHardwareState();
        var history = CreateHistoryStates(3);

        double[] features = _extractor.ExtractFeatures(current, history);

        Assert.Equal(20, features.Length);
    }

    [Fact]
    public void ExtractFeatures_FeaturesInValidRange()
    {
        var current = CreateTestHardwareState();
        var history = CreateHistoryStates(5);

        double[] features = _extractor.ExtractFeatures(current, history);

        for (int i = 0; i < features.Length; i++)
        {
            Assert.True(features[i] >= 0.0 && features[i] <= 1.0,
                $"Feature[{i}] = {features[i]} is out of [0,1] range");
        }
    }

    [Fact]
    public void ExtractFeatures_EmptyHistory_StillProducesValidOutput()
    {
        var current = CreateTestHardwareState();

        double[] features = _extractor.ExtractFeatures(current, Array.Empty<HardwareState>());

        Assert.Equal(20, features.Length);
        Assert.All(features, f => Assert.InRange(f, 0.0, 1.0));
    }

    [Fact]
    public void ExtractFeatures_NullCurrent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _extractor.ExtractFeatures(null!, Array.Empty<HardwareState>()));
    }

    [Fact]
    public void ExtractFeatures_NullHistory_ThrowsArgumentNullException()
    {
        var current = CreateTestHardwareState();
        Assert.Throws<ArgumentNullException>(() =>
            _extractor.ExtractFeatures(current, null!));
    }

    [Fact]
    public void ExtractFeatures_DifferentStates_ProduceDifferentFeatures()
    {
        var state1 = new HardwareState
        {
            Cpu = new CpuState { Temperature = 40, Usage = 10, Power = 50, ClockMHz = 2500 },
            Gpu = new GpuState { Temperature = 35, Usage = 5, Power = 30, CoreClockMHz = 1500 },
            Timestamp = DateTime.Now
        };

        var state2 = new HardwareState
        {
            Cpu = new CpuState { Temperature = 90, Usage = 95, Power = 200, ClockMHz = 4500 },
            Gpu = new GpuState { Temperature = 85, Usage = 90, Power = 250, CoreClockMHz = 2500 },
            Timestamp = DateTime.Now
        };

        double[] features1 = _extractor.ExtractFeatures(state1, Array.Empty<HardwareState>());
        double[] features2 = _extractor.ExtractFeatures(state2, Array.Empty<HardwareState>());

        bool allSame = features1.Zip(features2, (a, b) => Math.Abs(a - b) < 0.0001).All(x => x);
        Assert.False(allSame);
    }

    [Fact]
    public void ExtractFeatures_ExtremeValues_ClampedValidly()
    {
        var extreme = new HardwareState
        {
            Cpu = new CpuState { Temperature = 200, Usage = 150, Power = 500, ClockMHz = 10000 },
            Gpu = new GpuState { Temperature = 150, HotspotTemperature = 160, Usage = 120, Power = 400, CoreClockMHz = 5000, TotalVramMB = 16384, UsedVramMB = 32768 },
            Timestamp = DateTime.Now
        };

        double[] features = _extractor.ExtractFeatures(extreme, Array.Empty<HardwareState>());

        Assert.All(features, f => Assert.InRange(f, 0.0, 1.0));
    }

    [Fact]
    public void ExtractFeatures_WithHistory_ComputesDerivatives()
    {
        var history = new List<HardwareState>();
        var now = DateTime.Now;

        for (int i = 5; i > 0; i--)
        {
            history.Add(new HardwareState
            {
                Cpu = new CpuState { Temperature = 40 + i * 2, Usage = 50 + i * 5 },
                Gpu = new GpuState { Temperature = 35 + i * 3, Usage = 40 + i * 4 },
                Timestamp = now.AddSeconds(-i)
            });
        }

        var current = new HardwareState
        {
            Cpu = new CpuState { Temperature = 80, Usage = 90 },
            Gpu = new GpuState { Temperature = 75, Usage = 80 },
            Timestamp = now
        };

        double[] features = _extractor.ExtractFeatures(current, history.ToArray());

        Assert.Equal(20, features.Length);
        Assert.InRange(features[14], 0.0, 1.0);
        Assert.InRange(features[15], 0.0, 1.0);
    }

    [Fact]
    public void Constructor_InvalidWindow_ClampsToValidRange()
    {
        var extractor = new SensorFeatureExtractor(windowSeconds: -10, featureCount: 16);
        Assert.Equal(1, extractor.WindowSeconds);

        extractor = new SensorFeatureExtractor(windowSeconds: 1000, featureCount: 16);
        Assert.Equal(60, extractor.WindowSeconds);
    }

    private static HardwareState CreateTestHardwareState()
    {
        return new HardwareState
        {
            Cpu = new CpuState
            {
                Temperature = 65,
                Usage = 45,
                Power = 120,
                ClockMHz = 3200,
                PowerLimit = 200
            },
            Gpu = new GpuState
            {
                Temperature = 55,
                HotspotTemperature = 62,
                Usage = 60,
                Power = 180,
                UsedVramMB = 4096,
                TotalVramMB = 8192,
                CoreClockMHz = 1800,
                MemoryClockMHz = 5000
            },
            Battery = new BatteryState
            {
                ChargePercent = 75,
                IsCharging = true,
                ChargeLimit = 100,
                Status = "Discharging"
            },
            Fan = new FanState
            {
                CpuFanSpeed = 45,
                CpuFanRpm = 2400,
                GpuFanSpeed = 50,
                GpuFanRpm = 2600,
                MidFanSpeed = 40
            },
            Timestamp = DateTime.Now
        };
    }

    private static HardwareState[] CreateHistoryStates(int count)
    {
        var states = new HardwareState[count];
        var now = DateTime.Now;

        for (int i = 0; i < count; i++)
        {
            states[i] = new HardwareState
            {
                Cpu = new CpuState
                {
                    Temperature = 60 + i,
                    Usage = 40 + i * 2,
                    Power = 100 + i * 5,
                    ClockMHz = 3000 + i * 50
                },
                Gpu = new GpuState
                {
                    Temperature = 50 + i,
                    HotspotTemperature = 58 + i,
                    Usage = 55 + i * 2,
                    Power = 150 + i * 8,
                    CoreClockMHz = 1700 + i * 25
                },
                Timestamp = now.AddSeconds(-(count - i))
            };
        }

        return states;
    }
}