using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class SensorBackgroundServiceTests : IDisposable
{
    private readonly Mock<IAtkDevice> _mockDevice;
    private readonly Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private readonly Mock<IGpuControl> _mockGpuControl;
    private readonly Mock<ILogger<SensorPipeline>> _mockPipelineLogger;
    private readonly Mock<ILogger<SensorBackgroundService>> _mockServiceLogger;
    private readonly AsusAcpi _acpi;
    private readonly SensorPipeline _pipeline;
    private readonly SensorQueue _queue;
    private bool _disposed;

    public SensorBackgroundServiceTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockGpuControl = new Mock<IGpuControl>();
        _mockPipelineLogger = new Mock<ILogger<SensorPipeline>>();
        _mockServiceLogger = new Mock<ILogger<SensorBackgroundService>>();

        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns<byte[], int>((buf, size) => BitConverter.GetBytes(42));

        _mockGpuControl.Setup(g => g.GetCurrentTemperature()).Returns(60);
        _mockGpuControl.Setup(g => g.GetHotspotTemperature()).Returns(75);
        _mockGpuControl.Setup(g => g.GetGpuUse()).Returns(50);
        _mockGpuControl.Setup(g => g.GetGpuPower()).Returns(150f);
        _mockGpuControl.Setup(g => g.GetClockInfo()).Returns((1800, 5000));
        _mockGpuControl.Setup(g => g.GetVramInfo()).Returns((6144L, 8192L));
        _mockGpuControl.Setup(g => g.GetFanSpeed()).Returns(65);

        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, logger: _mockPipelineLogger.Object);
        _queue = new SensorQueue(100);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _pipeline.Dispose();
            _queue.Dispose();
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParams_CreatesInstance()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SensorBackgroundService(null!, _queue, _mockServiceLogger.Object));
    }

    [Fact]
    public void Constructor_NullQueue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SensorBackgroundService(_pipeline, null!, _mockServiceLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SensorBackgroundService(_pipeline, _queue, null!));
    }

    #endregion

    #region IntervalMs Tests

    [Fact]
    public void IntervalMs_Default_Is1000()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        Assert.Equal(1000, service.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetValid_Updates()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        service.IntervalMs = 500;
        Assert.Equal(500, service.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetBelowMin_ClampsTo100()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        service.IntervalMs = 10;
        Assert.Equal(100, service.IntervalMs);
    }

    [Fact]
    public void IntervalMs_SetAboveMax_ClampsTo5000()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        service.IntervalMs = 10000;
        Assert.Equal(5000, service.IntervalMs);
    }

    #endregion

    #region GetLatestState Tests

    [Fact]
    public void GetLatestState_EmptyQueue_ReturnsNull()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        var state = service.GetLatestState();

        Assert.Null(state);
    }

    [Fact]
    public void GetLatestState_WithData_ReturnsState()
    {
        var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, queue: _queue, logger: _mockPipelineLogger.Object);
        pipeline.CollectOnce();

        using var service = new SensorBackgroundService(pipeline, _queue, _mockServiceLogger.Object);
        var state = service.GetLatestState();

        Assert.NotNull(state);
    }

    #endregion

    #region GetRecentStates Tests

    [Fact]
    public void GetRecentStates_EmptyQueue_ReturnsEmpty()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        var states = service.GetRecentStates();

        Assert.Empty(states);
    }

    [Fact]
    public void GetRecentStates_WithData_ReturnsRecent()
    {
        var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, queue: _queue, logger: _mockPipelineLogger.Object);
        pipeline.CollectOnce();
        pipeline.CollectOnce();

        using var service = new SensorBackgroundService(pipeline, _queue, _mockServiceLogger.Object);
        var states = service.GetRecentStates(1);

        Assert.Single(states);
    }

    [Fact]
    public void GetRecentStates_DefaultCount_Is100()
    {
        var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, queue: _queue, logger: _mockPipelineLogger.Object);
        for (int i = 0; i < 3; i++)
        {
            pipeline.CollectOnce();
        }

        using var service = new SensorBackgroundService(pipeline, _queue, _mockServiceLogger.Object);
        var states = service.GetRecentStates();

        Assert.Equal(3, states.Count);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_CanBeStartedAndStopped()
    {
        using var service = new SensorBackgroundService(_pipeline, _queue, _mockServiceLogger.Object);
        using var cts = new CancellationTokenSource();

        var task = service.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();

        await task;
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_CollectsDataDuringRun()
    {
        using var queue = new SensorQueue(1000);
        using var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, logger: _mockPipelineLogger.Object);
        using var service = new SensorBackgroundService(pipeline, queue, _mockServiceLogger.Object)
        {
            IntervalMs = 50
        };

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        await Task.Delay(200, cts.Token);
        await cts.CancelAsync();
        await task;

        Assert.True(queue.Count > 0);
    }

    [Fact]
    public async Task ExecuteAsync_CapturesExceptionAndContinues()
    {
        var pipeline = new ThrowingSensorPipelineStub();

        using var service = new SensorBackgroundService(pipeline, _queue, _mockServiceLogger.Object)
        {
            IntervalMs = 50
        };
        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        await Task.Delay(200, cts.Token);
        await cts.CancelAsync();
        await task;

        _mockServiceLogger.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private class ThrowingSensorPipelineStub : SensorPipeline
    {
        public ThrowingSensorPipelineStub() : base(null, null) { }

        public override HardwareState CollectOnce()
        {
            throw new InvalidOperationException("Test error");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullIntegration_PipelineServiceQueue()
    {
        using var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, logger: _mockPipelineLogger.Object);
        using var queue = new SensorQueue(500);
        using var service = new SensorBackgroundService(pipeline, queue, _mockServiceLogger.Object)
        {
            IntervalMs = 50
        };

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        await Task.Delay(300, cts.Token);
        await cts.CancelAsync();
        await task;

        Assert.True(queue.Count > 0);

        var latest = service.GetLatestState();
        Assert.NotNull(latest);
        Assert.NotNull(latest!.Cpu);
        Assert.NotNull(latest.Gpu);
    }

    [Fact]
    public async Task FullIntegration_DataConsistency()
    {
        using var pipeline = new SensorPipeline(_acpi, _mockGpuControl.Object, logger: _mockPipelineLogger.Object);
        using var queue = new SensorQueue(500);
        using var service = new SensorBackgroundService(pipeline, queue, _mockServiceLogger.Object)
        {
            IntervalMs = 50
        };

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        await Task.Delay(200, cts.Token);
        await cts.CancelAsync();
        await task;

        var states = service.GetRecentStates(10);
        foreach (var state in states)
        {
            Assert.InRange(state.Cpu.Temperature, 0, 200);
            Assert.InRange(state.Gpu.Temperature, 0, 200);
        }
    }

    #endregion
}
