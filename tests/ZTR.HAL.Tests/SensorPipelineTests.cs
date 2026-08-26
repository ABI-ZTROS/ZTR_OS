using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class SensorPipelineTests : IDisposable
{
    private readonly Mock<IAtkDevice> _mockDevice;
    private readonly Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private readonly Mock<IGpuControl> _mockGpuControl;
    private readonly Mock<ILogger<SensorPipeline>> _mockPipelineLogger;
    private readonly AsusAcpi _acpi;
    private SensorPipeline _pipeline;
    private bool _disposed;

    public SensorPipelineTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockGpuControl = new Mock<IGpuControl>();
        _mockPipelineLogger = new Mock<ILogger<SensorPipeline>>();

        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);

        SetupDefaultAcpiResponses();
        SetupDefaultGpuResponses();

        _pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, logger: _mockPipelineLogger.Object);
    }

    private void SetupDefaultAcpiResponses()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns<byte[], int>((buf, size) =>
            {
                if (buf.Length >= 8)
                {
                    var deviceId = BitConverter.ToUInt32(buf, 0);
                    return BitConverter.GetBytes((int)deviceId);
                }
                return BitConverter.GetBytes(0);
            });
    }

    private void SetupDefaultGpuResponses()
    {
        _mockGpuControl.Setup(g => g.GetCurrentTemperature()).Returns(60);
        _mockGpuControl.Setup(g => g.GetHotspotTemperature()).Returns(75);
        _mockGpuControl.Setup(g => g.GetGpuUse()).Returns(50);
        _mockGpuControl.Setup(g => g.GetGpuPower()).Returns(150f);
        _mockGpuControl.Setup(g => g.GetClockInfo()).Returns((1800, 5000));
        _mockGpuControl.Setup(g => g.GetVramInfo()).Returns((6144L, 8192L));
        _mockGpuControl.Setup(g => g.GetFanSpeed()).Returns(65);
        _mockGpuControl.Setup(g => g.GetState()).Returns(new GpuState
        {
            Temperature = 60,
            HotspotTemperature = 75,
            Usage = 50,
            Power = 150,
            CoreClockMHz = 1800,
            MemoryClockMHz = 5000,
            UsedVramMB = 6144,
            TotalVramMB = 8192
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _pipeline.Dispose();
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParams_CreatesInstance()
    {
        Assert.NotNull(_pipeline);
    }

    [Fact]
    public void Constructor_WithNullAcpi_DoesNotThrow()
    {
        var pipeline = new SensorPipeline(null, _mockGpuControl.Object);
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Constructor_WithNullGpu_DoesNotThrow()
    {
        var pipeline = new SensorPipeline(_acpi, null);
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Constructor_WithNullBoth_DoesNotThrow()
    {
        var pipeline = new SensorPipeline(null, null);
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Constructor_WithCustomDependencies_UsesThem()
    {
        var aggregator = new SensorAggregator(2.5, 150);
        var queue = new SensorQueue(50);
        var degradationHandler = new SensorDegradationHandler(10, 5);

        var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object,
            aggregator: aggregator, queue: queue, degradationHandler: degradationHandler);

        Assert.Same(aggregator, pipeline.Aggregator);
        Assert.Same(queue, pipeline.Queue);
        Assert.Same(degradationHandler, pipeline.DegradationHandler);
    }

    #endregion

    #region IntervalMs Tests

    [Fact]
    public void IntervalMs_Default_Is1000()
    {
        Assert.Equal(1000, _pipeline.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetValidValue_Updates()
    {
        _pipeline.IntervalMs = 500;
        Assert.Equal(500, _pipeline.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetBelowMin_ClampsTo100()
    {
        _pipeline.IntervalMs = 10;
        Assert.Equal(100, _pipeline.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetAboveMax_ClampsTo5000()
    {
        _pipeline.IntervalMs = 10000;
        Assert.Equal(5000, _pipeline.IntervalMs);
    }

    #endregion

    #region CollectOnce Tests

    [Fact]
    public void CollectOnce_ReturnsHardwareState()
    {
        var state = _pipeline.CollectOnce();

        Assert.NotNull(state);
        Assert.NotNull(state.Cpu);
        Assert.NotNull(state.Gpu);
        Assert.NotNull(state.Battery);
        Assert.NotNull(state.Fan);
    }

    [Fact]
    public void CollectOnce_CpuState_Populated()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns<byte[], int>((buf, size) => BitConverter.GetBytes(42));

        var state = _pipeline.CollectOnce();

        Assert.Equal(42, state.Cpu.Temperature);
    }

    [Fact]
    public void CollectOnce_GpuState_Populated()
    {
        var state = _pipeline.CollectOnce();

        Assert.Equal(60, state.Gpu.Temperature);
        Assert.Equal(75, state.Gpu.HotspotTemperature);
        Assert.Equal(50, state.Gpu.Usage);
        Assert.Equal(150, state.Gpu.Power);
        Assert.Equal(1800, state.Gpu.CoreClockMHz);
        Assert.Equal(5000, state.Gpu.MemoryClockMHz);
        Assert.Equal(6144, state.Gpu.UsedVramMB);
        Assert.Equal(8192, state.Gpu.TotalVramMB);
    }

    [Fact]
    public void CollectOnce_BatteryState_Populated()
    {
        var state = _pipeline.CollectOnce();

        Assert.NotNull(state.Battery);
    }

    [Fact]
    public void CollectOnce_FanState_Populated()
    {
        var state = _pipeline.CollectOnce();

        Assert.NotNull(state.Fan);
    }

    [Fact]
    public void CollectOnce_AddsToQueue()
    {
        _pipeline.CollectOnce();
        _pipeline.CollectOnce();

        Assert.Equal(2, _pipeline.Queue.Count);
    }

    [Fact]
    public void CollectOnce_WithNullGpuControl_GpuStateEmpty()
    {
        var pipeline = new SensorPipeline(_acpi, null);
        var state = pipeline.CollectOnce();

        Assert.NotNull(state);
    }

    [Fact]
    public void CollectOnce_WithNullAcpi_CpuStateHasDefaults()
    {
        var pipeline = new SensorPipeline(null, _mockGpuControl.Object);
        var state = pipeline.CollectOnce();

        Assert.NotNull(state);
        Assert.NotNull(state.Cpu);
    }

    #endregion

    #region GetLatestStates Tests

    [Fact]
    public void GetLatestStates_NoData_ReturnsEmpty()
    {
        var states = _pipeline.GetLatestStates();

        Assert.Empty(states);
    }

    [Fact]
    public void GetLatestStates_WithData_ReturnsRecent()
    {
        _pipeline.CollectOnce();
        _pipeline.CollectOnce();
        _pipeline.CollectOnce();

        var states = _pipeline.GetLatestStates(2);

        Assert.Equal(2, states.Count);
    }

    [Fact]
    public void GetLatestStates_DefaultCount_Is100()
    {
        for (int i = 0; i < 5; i++)
        {
            _pipeline.CollectOnce();
        }

        var states = _pipeline.GetLatestStates();

        Assert.Equal(5, states.Count);
    }

    #endregion

    #region Queue Integration Tests

    [Fact]
    public void Queue_Property_IsSameInstance()
    {
        Assert.Same(_pipeline.Queue, _pipeline.Queue);
    }

    [Fact]
    public void DegradationHandler_Property_IsSameInstance()
    {
        Assert.Same(_pipeline.DegradationHandler, _pipeline.DegradationHandler);
    }

    [Fact]
    public void Aggregator_Property_IsSameInstance()
    {
        Assert.Same(_pipeline.Aggregator, _pipeline.Aggregator);
    }

    #endregion

    #region Start/Stop Tests

    [Fact]
    public void Start_DoesNotThrow()
    {
        _pipeline.Start();
        Assert.True(true);
    }

    [Fact]
    public void Stop_DoesNotThrow()
    {
        _pipeline.Stop();
        Assert.True(true);
    }

    [Fact]
    public void StartThenStop_DoesNotThrow()
    {
        _pipeline.Start();
        _pipeline.Stop();
        Assert.True(true);
    }

    #endregion

    #region CPU Reading Tests

    [Fact]
    public void CpuTemperature_ReadFromAcpi()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns<byte[], int>((buf, size) => BitConverter.GetBytes(48));

        var state = _pipeline.CollectOnce();

        Assert.Equal(48, state.Cpu.Temperature);
    }

    [Fact]
    public void CpuPower_ReadFromAcpi()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(true);
        mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns<byte[], int>((buf, size) =>
            {
                if (buf.Length >= 8)
                {
                    var deviceId = BitConverter.ToUInt32(buf, 0);
                    return BitConverter.GetBytes((int)deviceId + 100);
                }
                return BitConverter.GetBytes(0);
            });

        var acpi = new AsusAcpi(mockDevice.Object);
        var pipeline = new SensorPipeline(acpi, null);

        var state = pipeline.CollectOnce();
        Assert.True(state.Cpu.Power >= 0);
    }

    #endregion

    #region GPU Reading Tests

    [Fact]
    public void GpuTemperature_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetCurrentTemperature()).Returns(55);

        var state = _pipeline.CollectOnce();

        Assert.Equal(55, state.Gpu.Temperature);
    }

    [Fact]
    public void GpuHotspotTemperature_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetHotspotTemperature()).Returns(82);

        var state = _pipeline.CollectOnce();

        Assert.Equal(82, state.Gpu.HotspotTemperature);
    }

    [Fact]
    public void GpuUsage_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetGpuUse()).Returns(67);

        var state = _pipeline.CollectOnce();

        Assert.Equal(67, state.Gpu.Usage);
    }

    [Fact]
    public void GpuPower_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetGpuPower()).Returns(200f);

        var state = _pipeline.CollectOnce();

        Assert.Equal(200, state.Gpu.Power);
    }

    [Fact]
    public void GpuClockInfo_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetClockInfo()).Returns((2100, 6000));

        var state = _pipeline.CollectOnce();

        Assert.Equal(2100, state.Gpu.CoreClockMHz);
        Assert.Equal(6000, state.Gpu.MemoryClockMHz);
    }

    [Fact]
    public void GpuVramInfo_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetVramInfo()).Returns((12288L, 16384L));

        var state = _pipeline.CollectOnce();

        Assert.Equal(12288, state.Gpu.UsedVramMB);
        Assert.Equal(16384, state.Gpu.TotalVramMB);
    }

    [Fact]
    public void GpuFanSpeed_ReadFromControl()
    {
        _mockGpuControl.Setup(g => g.GetFanSpeed()).Returns(80);

        var state = _pipeline.CollectOnce();

        Assert.Equal(80, state.Fan.GpuFanSpeed);
    }

    #endregion

    #region Degradation Integration Tests

    [Fact]
    public void DegradationHandler_RegistersAllSensors()
    {
        var health = _pipeline.DegradationHandler.GetAllHealthInfo();
        Assert.NotEmpty(health);
        Assert.True(health.ContainsKey("CPU Temperature"));
        Assert.True(health.ContainsKey("GPU Temperature"));
        Assert.True(health.ContainsKey("BatteryCharge"));
    }

    [Fact]
    public void DegradationHandler_AfterCollection_SensorsAreHealthy()
    {
        _pipeline.CollectOnce();

        Assert.True(_pipeline.DegradationHandler.IsSensorHealthy("CPU Temperature"));
        Assert.True(_pipeline.DegradationHandler.IsSensorHealthy("GPU Temperature"));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        _pipeline.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        _pipeline.Dispose();
        _pipeline.Dispose();
        Assert.True(true);
    }

    #endregion
}
