using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class NvidiaGpuControlTests : IDisposable
{
    private readonly Mock<INvApiGpu> _mockNvApi;
    private readonly NvidiaGpuControl _control;
    private bool _disposed;

    public NvidiaGpuControlTests()
    {
        _mockNvApi = new Mock<INvApiGpu>();
        _mockNvApi.Setup(n => n.IsAvailable).Returns(true);
        _mockNvApi.Setup(n => n.GpuCount).Returns(2);
        _mockNvApi.Setup(n => n.GetGpuName(0)).Returns("NVIDIA GeForce RTX 4090");
        _control = new NvidiaGpuControl(0, _mockNvApi.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    [Fact]
    public void Constructor_WhenNvApiAvailable_SetsIsValidTrue()
    {
        Assert.True(_control.IsValid);
    }

    [Fact]
    public void Constructor_WhenNvApiUnavailable_SetsIsValidFalse()
    {
        var mockNvApi = new Mock<INvApiGpu>();
        mockNvApi.Setup(n => n.IsAvailable).Returns(false);
        mockNvApi.Setup(n => n.GpuCount).Returns(0);

        using var control = new NvidiaGpuControl(0, mockNvApi.Object);

        Assert.False(control.IsValid);
    }

    [Fact]
    public void FullName_ReturnsGpuNameFromApi()
    {
        Assert.Equal("NVIDIA GeForce RTX 4090", _control.FullName);
    }

    [Fact]
    public void IsNvidia_ReturnsTrue()
    {
        Assert.True(_control.IsNvidia);
    }

    [Fact]
    public void IsAmd_ReturnsFalse()
    {
        Assert.False(_control.IsAmd);
    }

    [Fact]
    public void GpuIndex_ReturnsConstructorValue()
    {
        Assert.Equal(0, _control.GpuIndex);
    }

    [Fact]
    public void GetCurrentTemperature_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetTemperature(0)).Returns(65);

        var result = _control.GetCurrentTemperature();

        Assert.Equal(65, result);
    }

    [Fact]
    public void GetCurrentTemperature_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetTemperature(0)).Returns((int?)null);

        var result = _control.GetCurrentTemperature();

        Assert.Null(result);
    }

    [Fact]
    public void GetHotspotTemperature_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetHotspotTemperature(0)).Returns(78);

        var result = _control.GetHotspotTemperature();

        Assert.Equal(78, result);
    }

    [Fact]
    public void GetHotspotTemperature_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetHotspotTemperature(0)).Returns((int?)null);

        var result = _control.GetHotspotTemperature();

        Assert.Null(result);
    }

    [Fact]
    public void GetGpuUse_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetUsage(0)).Returns(42);

        var result = _control.GetGpuUse();

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetGpuUse_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetUsage(0)).Returns((int?)null);

        var result = _control.GetGpuUse();

        Assert.Null(result);
    }

    [Fact]
    public void GetVramInfo_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetVramInfo(0)).Returns((2048L, 8192L));

        var result = _control.GetVramInfo();

        Assert.NotNull(result);
        Assert.Equal(2048, result.Value.usedMb);
        Assert.Equal(8192, result.Value.totalMb);
    }

    [Fact]
    public void GetVramInfo_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetVramInfo(0)).Returns(((long usedMb, long totalMb)?)null);

        var result = _control.GetVramInfo();

        Assert.Null(result);
    }

    [Fact]
    public void GetGpuPower_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetPower(0)).Returns(150.5f);

        var result = _control.GetGpuPower();

        Assert.Equal(150.5f, result);
    }

    [Fact]
    public void GetGpuPower_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetPower(0)).Returns((float?)null);

        var result = _control.GetGpuPower();

        Assert.Null(result);
    }

    [Fact]
    public void SetClocks_ReturnsApiResult()
    {
        _mockNvApi.Setup(n => n.SetClocks(0, 100, 50)).Returns(true);

        var result = _control.SetClocks(100, 50);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetClocks(0, 100, 50), Times.Once);
    }

    [Fact]
    public void SetClocks_WhenApiReturnsFalse_ReturnsFalse()
    {
        _mockNvApi.Setup(n => n.SetClocks(0, 100, 50)).Returns(false);

        var result = _control.SetClocks(100, 50);

        Assert.False(result);
    }

    [Fact]
    public void ResetClocks_ReturnsApiResult()
    {
        _mockNvApi.Setup(n => n.ResetClocks(0)).Returns(true);

        var result = _control.ResetClocks();

        Assert.True(result);
        _mockNvApi.Verify(n => n.ResetClocks(0), Times.Once);
    }

    [Fact]
    public void ResetClocks_WhenApiReturnsFalse_ReturnsFalse()
    {
        _mockNvApi.Setup(n => n.ResetClocks(0)).Returns(false);

        var result = _control.ResetClocks();

        Assert.False(result);
    }

    [Fact]
    public void SetPowerLimit_ReturnsApiResult()
    {
        _mockNvApi.Setup(n => n.SetPowerLimit(0, 300)).Returns(true);

        var result = _control.SetPowerLimit(300);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetPowerLimit(0, 300), Times.Once);
    }

    [Fact]
    public void SetPowerLimit_WhenApiReturnsFalse_ReturnsFalse()
    {
        _mockNvApi.Setup(n => n.SetPowerLimit(0, 500)).Returns(false);

        var result = _control.SetPowerLimit(500);

        Assert.False(result);
    }

    [Fact]
    public void SetMaxGpuClock_ReturnsApiResult()
    {
        _mockNvApi.Setup(n => n.SetMaxGpuClock(0, 2500)).Returns(true);

        var result = _control.SetMaxGpuClock(2500);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetMaxGpuClock(0, 2500), Times.Once);
    }

    [Fact]
    public void GetClockInfo_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetClockInfo(0)).Returns((2500, 10000));

        var result = _control.GetClockInfo();

        Assert.NotNull(result);
        Assert.Equal(2500, result.Value.coreClockMHz);
        Assert.Equal(10000, result.Value.memoryClockMHz);
    }

    [Fact]
    public void GetClockInfo_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetClockInfo(0)).Returns(((int core, int mem)?)null);

        var result = _control.GetClockInfo();

        Assert.Null(result);
    }

    [Fact]
    public void SetFanSpeed_ClampsValueAndCallsApi()
    {
        _mockNvApi.Setup(n => n.SetFanSpeed(0, 100)).Returns(true);

        var result = _control.SetFanSpeed(150);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetFanSpeed(0, 100), Times.Once);
    }

    [Fact]
    public void SetFanSpeed_ClampsNegativeToZero()
    {
        _mockNvApi.Setup(n => n.SetFanSpeed(0, 0)).Returns(true);

        var result = _control.SetFanSpeed(-10);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetFanSpeed(0, 0), Times.Once);
    }

    [Fact]
    public void SetFanSpeed_ValidPassThrough()
    {
        _mockNvApi.Setup(n => n.SetFanSpeed(0, 75)).Returns(true);

        var result = _control.SetFanSpeed(75);

        Assert.True(result);
        _mockNvApi.Verify(n => n.SetFanSpeed(0, 75), Times.Once);
    }

    [Fact]
    public void GetFanSpeed_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetFanSpeed(0)).Returns(65);

        var result = _control.GetFanSpeed();

        Assert.Equal(65, result);
    }

    [Fact]
    public void GetPowerLimitRange_ReturnsApiValue()
    {
        _mockNvApi.Setup(n => n.GetPowerLimitRange(0)).Returns((100, 450));

        var result = _control.GetPowerLimitRange();

        Assert.NotNull(result);
        Assert.Equal(100, result.Value.minWatts);
        Assert.Equal(450, result.Value.maxWatts);
    }

    [Fact]
    public void GetPowerLimitRange_WhenApiReturnsNull_ReturnsNull()
    {
        _mockNvApi.Setup(n => n.GetPowerLimitRange(0)).Returns(((int min, int max)?)null);

        var result = _control.GetPowerLimitRange();

        Assert.Null(result);
    }

    [Fact]
    public void GetSupportedPowerModes_ReturnsApiValue()
    {
        var modes = new List<string> { "Performance", "PowerSave", "Default" };
        _mockNvApi.Setup(n => n.GetSupportedPowerModes(0)).Returns(modes);

        var result = _control.GetSupportedPowerModes();

        Assert.Equal(3, result.Count);
        Assert.Contains("Performance", result);
    }

    [Fact]
    public void KillGpuApps_CallsApi()
    {
        _control.KillGpuApps();

        _mockNvApi.Verify(n => n.KillGpuApps(0), Times.Once);
    }

    [Fact]
    public void GetState_ReturnsCompleteGpuState()
    {
        _mockNvApi.Setup(n => n.GetTemperature(0)).Returns(70);
        _mockNvApi.Setup(n => n.GetHotspotTemperature(0)).Returns(85);
        _mockNvApi.Setup(n => n.GetUsage(0)).Returns(55);
        _mockNvApi.Setup(n => n.GetPower(0)).Returns(120.5f);
        _mockNvApi.Setup(n => n.GetVramInfo(0)).Returns((3072L, 12288L));
        _mockNvApi.Setup(n => n.GetClockInfo(0)).Returns((1800, 5000));

        var state = _control.GetState();

        Assert.Equal(70, state.Temperature);
        Assert.Equal(85, state.HotspotTemperature);
        Assert.Equal(55, state.Usage);
        Assert.Equal(120, state.Power);
        Assert.Equal(3072, state.UsedVramMB);
        Assert.Equal(12288, state.TotalVramMB);
        Assert.Equal(1800, state.CoreClockMHz);
        Assert.Equal(5000, state.MemoryClockMHz);
    }

    [Fact]
    public void GetState_WhenApiReturnsNulls_UsesDefaults()
    {
        _mockNvApi.Setup(n => n.GetTemperature(0)).Returns((int?)null);
        _mockNvApi.Setup(n => n.GetHotspotTemperature(0)).Returns((int?)null);
        _mockNvApi.Setup(n => n.GetUsage(0)).Returns((int?)null);
        _mockNvApi.Setup(n => n.GetPower(0)).Returns((float?)null);
        _mockNvApi.Setup(n => n.GetVramInfo(0)).Returns(((long, long)?)null);
        _mockNvApi.Setup(n => n.GetClockInfo(0)).Returns(((int, int)?)null);

        var state = _control.GetState();

        Assert.Equal(0, state.Temperature);
        Assert.Equal(0, state.HotspotTemperature);
        Assert.Equal(0, state.Usage);
        Assert.Equal(0, state.Power);
        Assert.Equal(0, state.UsedVramMB);
        Assert.Equal(0, state.TotalVramMB);
        Assert.Equal(0, state.CoreClockMHz);
        Assert.Equal(0, state.MemoryClockMHz);
    }

    [Fact]
    public void Constructor_WhenGpuIndexOutOfRange_SetsIsValidFalse()
    {
        var mockNvApi = new Mock<INvApiGpu>();
        mockNvApi.Setup(n => n.IsAvailable).Returns(true);
        mockNvApi.Setup(n => n.GpuCount).Returns(1);

        using var control = new NvidiaGpuControl(5, mockNvApi.Object);

        Assert.False(control.IsValid);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _control.Dispose();
        _control.Dispose();
    }

    [Fact]
    public void AllSensorMethods_WhenNvApiThrows_ReturnsNull()
    {
        var mockNvApi = new Mock<INvApiGpu>();
        mockNvApi.Setup(n => n.IsAvailable).Returns(true);
        mockNvApi.Setup(n => n.GpuCount).Returns(1);
        mockNvApi.Setup(n => n.GetGpuName(0)).Returns("Test GPU");
        mockNvApi.Setup(n => n.GetTemperature(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.GetHotspotTemperature(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.GetUsage(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.GetVramInfo(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.GetPower(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.GetClockInfo(0)).Throws<InvalidOperationException>();

        using var control = new NvidiaGpuControl(0, mockNvApi.Object);

        Assert.Null(control.GetCurrentTemperature());
        Assert.Null(control.GetHotspotTemperature());
        Assert.Null(control.GetGpuUse());
        Assert.Null(control.GetVramInfo());
        Assert.Null(control.GetGpuPower());
        Assert.Null(control.GetClockInfo());
    }

    [Fact]
    public void AllControlMethods_WhenNvApiThrows_ReturnsFalse()
    {
        var mockNvApi = new Mock<INvApiGpu>();
        mockNvApi.Setup(n => n.IsAvailable).Returns(true);
        mockNvApi.Setup(n => n.GpuCount).Returns(1);
        mockNvApi.Setup(n => n.GetGpuName(0)).Returns("Test GPU");
        mockNvApi.Setup(n => n.SetClocks(0, 0, 0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.ResetClocks(0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.SetPowerLimit(0, 0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.SetMaxGpuClock(0, 0)).Throws<InvalidOperationException>();
        mockNvApi.Setup(n => n.SetFanSpeed(0, 0)).Throws<InvalidOperationException>();

        using var control = new NvidiaGpuControl(0, mockNvApi.Object);

        Assert.False(control.SetClocks(0, 0));
        Assert.False(control.ResetClocks());
        Assert.False(control.SetPowerLimit(0));
        Assert.False(control.SetMaxGpuClock(0));
        Assert.False(control.SetFanSpeed(0));
    }
}