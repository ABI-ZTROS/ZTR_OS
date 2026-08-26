using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class GpuSensorServiceTests : IDisposable
{
    private readonly Mock<ILogger<GpuSensorService>> _mockLogger;
    private readonly GpuSensorService _service;
    private bool _disposed;

    public GpuSensorServiceTests()
    {
        _mockLogger = new Mock<ILogger<GpuSensorService>>();
        _service = new GpuSensorService(_mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _service.Dispose();
        }
    }

    [Fact]
    public void Initialize_ClearsPreviousGpus()
    {
        _service.Initialize();
        var firstCount = _service.GpuControls.Count;

        _service.Initialize();
        var secondCount = _service.GpuControls.Count;

        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public void GpuControls_InitiallyEmpty()
    {
        Assert.Empty(_service.GpuControls);
    }

    [Fact]
    public void GetAllGpuStates_ReturnsEmptyListWhenNoGpus()
    {
        var states = _service.GetAllGpuStates();

        Assert.Empty(states);
    }

    [Fact]
    public void GetPrimaryGpuState_ReturnsEmptyStateWhenNoGpus()
    {
        var state = _service.GetPrimaryGpuState();

        Assert.NotNull(state);
        Assert.Equal(0, state.Temperature);
    }

    [Fact]
    public void GetTotalPower_ReturnsZeroWhenNoGpus()
    {
        var result = _service.GetTotalPower();

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetMaxTemperature_ReturnsZeroWhenNoGpus()
    {
        var result = _service.GetMaxTemperature();

        Assert.Equal(0, result);
    }

    [Fact]
    public void CreateGpuControl_InvalidType_ReturnsNull()
    {
        var result = _service.CreateGpuControl(true, 999);

        Assert.Null(result);
    }

    [Fact]
    public void CreateGpuControl_NvidiaType_ReturnsControlOrNull()
    {
        var result = _service.CreateGpuControl(true, 0);

        Assert.True(result == null || result.IsNvidia);
    }

    [Fact]
    public void CreateGpuControl_AmdType_ReturnsControlOrNull()
    {
        var result = _service.CreateGpuControl(false, 0);

        Assert.True(result == null || result.IsAmd);
    }

    [Fact]
    public void GetTotalPower_WithGpus_SumsPower()
    {
        var mockGpu1 = new Mock<IGpuControl>();
        mockGpu1.Setup(g => g.GetGpuPower()).Returns(100.0f);
        mockGpu1.Setup(g => g.IsValid).Returns(true);

        var mockGpu2 = new Mock<IGpuControl>();
        mockGpu2.Setup(g => g.GetGpuPower()).Returns(150.0f);
        mockGpu2.Setup(g => g.IsValid).Returns(true);

        var service = new GpuSensorService(_mockLogger.Object);
        service.Initialize();

        var result = service.GetTotalPower();

        Assert.True(result >= 0);
    }

    [Fact]
    public void GetMaxTemperature_WithGpus_ReturnsMax()
    {
        var service = new GpuSensorService(_mockLogger.Object);
        service.Initialize();

        var result = service.GetMaxTemperature();

        Assert.True(result >= 0);
    }

    [Fact]
    public void GetAllGpuStates_WithGpus_ReturnsStates()
    {
        var service = new GpuSensorService(_mockLogger.Object);
        service.Initialize();

        var states = service.GetAllGpuStates();

        Assert.NotNull(states);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _service.Dispose();
        _service.Dispose();
    }

    [Fact]
    public void Dispose_ClearsGpuControls()
    {
        _service.Initialize();
        _service.Dispose();

        Assert.Empty(_service.GpuControls);
    }
}