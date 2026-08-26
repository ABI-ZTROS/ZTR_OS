using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AmdGpuControlTests : IDisposable
{
    private readonly Mock<IAdl2Gpu> _mockAdl2;
    private readonly AmdGpuControl _control;
    private bool _disposed;

    public AmdGpuControlTests()
    {
        _mockAdl2 = new Mock<IAdl2Gpu>();
        _mockAdl2.Setup(n => n.IsAvailable).Returns(true);
        _mockAdl2.Setup(n => n.GpuCount).Returns(2);
        _mockAdl2.Setup(n => n.GetGpuName(0)).Returns("AMD Radeon RX 7900 XTX");
        _control = new AmdGpuControl(0, _mockAdl2.Object);
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
    public void Constructor_WhenAdl2Available_SetsIsValidTrue()
    {
        Assert.True(_control.IsValid);
    }

    [Fact]
    public void Constructor_WhenAdl2Unavailable_SetsIsValidFalse()
    {
        var mockAdl2 = new Mock<IAdl2Gpu>();
        mockAdl2.Setup(n => n.IsAvailable).Returns(false);
        mockAdl2.Setup(n => n.GpuCount).Returns(0);

        using var control = new AmdGpuControl(0, mockAdl2.Object);

        Assert.False(control.IsValid);
    }

    [Fact]
    public void FullName_ReturnsGpuNameFromApi()
    {
        Assert.Equal("AMD Radeon RX 7900 XTX", _control.FullName);
    }

    [Fact]
    public void IsNvidia_ReturnsFalse()
    {
        Assert.False(_control.IsNvidia);
    }

    [Fact]
    public void IsAmd_ReturnsTrue()
    {
        Assert.True(_control.IsAmd);
    }

    [Fact]
    public void GpuIndex_ReturnsConstructorValue()
    {
        Assert.Equal(0, _control.GpuIndex);
    }

    [Fact]
    public void GetCurrentTemperature_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetTemperature(0)).Returns(58);

        var result = _control.GetCurrentTemperature();

        Assert.Equal(58, result);
    }

    [Fact]
    public void GetCurrentTemperature_WhenApiReturnsNull_ReturnsNull()
    {
        _mockAdl2.Setup(n => n.GetTemperature(0)).Returns((int?)null);

        var result = _control.GetCurrentTemperature();

        Assert.Null(result);
    }

    [Fact]
    public void GetHotspotTemperature_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetHotspotTemperature(0)).Returns(72);

        var result = _control.GetHotspotTemperature();

        Assert.Equal(72, result);
    }

    [Fact]
    public void GetGpuUse_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetUsage(0)).Returns(35);

        var result = _control.GetGpuUse();

        Assert.Equal(35, result);
    }

    [Fact]
    public void GetVramInfo_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetVramInfo(0)).Returns((4096L, 16384L));

        var result = _control.GetVramInfo();

        Assert.NotNull(result);
        Assert.Equal(4096, result.Value.usedMb);
        Assert.Equal(16384, result.Value.totalMb);
    }

    [Fact]
    public void GetGpuPower_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetPower(0)).Returns(220.0f);

        var result = _control.GetGpuPower();

        Assert.Equal(220.0f, result);
    }

    [Fact]
    public void SetClocks_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.SetClocks(0, 150, 75)).Returns(true);

        var result = _control.SetClocks(150, 75);

        Assert.True(result);
        _mockAdl2.Verify(n => n.SetClocks(0, 150, 75), Times.Once);
    }

    [Fact]
    public void ResetClocks_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.ResetClocks(0)).Returns(true);

        var result = _control.ResetClocks();

        Assert.True(result);
    }

    [Fact]
    public void SetPowerLimit_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.SetPowerLimit(0, 350)).Returns(true);

        var result = _control.SetPowerLimit(350);

        Assert.True(result);
    }

    [Fact]
    public void SetFPSLimit_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.SetFPSLimit(0, 144)).Returns(true);

        var result = _control.SetFPSLimit(144);

        Assert.True(result);
        _mockAdl2.Verify(n => n.SetFPSLimit(0, 144), Times.Once);
    }

    [Fact]
    public void GetFPSLimit_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetFPSLimit(0)).Returns(60);

        var result = _control.GetFPSLimit();

        Assert.Equal(60, result);
    }

    [Fact]
    public void GetFPSLimit_WhenApiReturnsNull_ReturnsNull()
    {
        _mockAdl2.Setup(n => n.GetFPSLimit(0)).Returns((int?)null);

        var result = _control.GetFPSLimit();

        Assert.Null(result);
    }

    [Fact]
    public void SetiGpuPower_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.SetiGpuPower(0, 15000)).Returns(true);

        var result = _control.SetiGpuPower(15000);

        Assert.True(result);
        _mockAdl2.Verify(n => n.SetiGpuPower(0, 15000), Times.Once);
    }

    [Fact]
    public void GetiGpuSensors_ReturnsApiValue()
    {
        var sensors = new Dictionary<string, double>
        {
            ["Temperature"] = 55.0,
            ["Usage"] = 30.0,
            ["CoreClockMHz"] = 1200.0
        };
        _mockAdl2.Setup(n => n.GetiGpuSensors(0)).Returns(sensors);

        var result = _control.GetiGpuSensors();

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(55.0, result["Temperature"]);
        Assert.Equal(30.0, result["Usage"]);
    }

    [Fact]
    public void GetiGpuSensors_WhenApiReturnsNull_ReturnsNull()
    {
        _mockAdl2.Setup(n => n.GetiGpuSensors(0)).Returns((IReadOnlyDictionary<string, double>?)null);

        var result = _control.GetiGpuSensors();

        Assert.Null(result);
    }

    [Fact]
    public void GetPowerLimitRange_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetPowerLimitRange(0)).Returns((80, 400));

        var result = _control.GetPowerLimitRange();

        Assert.NotNull(result);
        Assert.Equal(80, result.Value.minWatts);
        Assert.Equal(400, result.Value.maxWatts);
    }

    [Fact]
    public void SetFanSpeed_ReturnsApiResult()
    {
        _mockAdl2.Setup(n => n.SetFanSpeed(0, 80)).Returns(true);

        var result = _control.SetFanSpeed(80);

        Assert.True(result);
    }

    [Fact]
    public void SetFanSpeed_ClampsAbove100()
    {
        _mockAdl2.Setup(n => n.SetFanSpeed(0, 100)).Returns(true);

        var result = _control.SetFanSpeed(200);

        Assert.True(result);
        _mockAdl2.Verify(n => n.SetFanSpeed(0, 100), Times.Once);
    }

    [Fact]
    public void SetFanSpeed_ClampsBelow0()
    {
        _mockAdl2.Setup(n => n.SetFanSpeed(0, 0)).Returns(true);

        var result = _control.SetFanSpeed(-5);

        Assert.True(result);
        _mockAdl2.Verify(n => n.SetFanSpeed(0, 0), Times.Once);
    }

    [Fact]
    public void GetFanSpeed_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetFanSpeed(0)).Returns(45);

        var result = _control.GetFanSpeed();

        Assert.Equal(45, result);
    }

    [Fact]
    public void GetClockInfo_ReturnsApiValue()
    {
        _mockAdl2.Setup(n => n.GetClockInfo(0)).Returns((1800, 9000));

        var result = _control.GetClockInfo();

        Assert.NotNull(result);
        Assert.Equal(1800, result.Value.coreClockMHz);
        Assert.Equal(9000, result.Value.memoryClockMHz);
    }

    [Fact]
    public void KillGpuApps_CallsApi()
    {
        _control.KillGpuApps();

        _mockAdl2.Verify(n => n.KillGpuApps(0), Times.Once);
    }

    [Fact]
    public void GetState_ReturnsCompleteGpuState()
    {
        _mockAdl2.Setup(n => n.GetTemperature(0)).Returns(60);
        _mockAdl2.Setup(n => n.GetHotspotTemperature(0)).Returns(75);
        _mockAdl2.Setup(n => n.GetUsage(0)).Returns(48);
        _mockAdl2.Setup(n => n.GetPower(0)).Returns(180.0f);
        _mockAdl2.Setup(n => n.GetVramInfo(0)).Returns((2048L, 8192L));
        _mockAdl2.Setup(n => n.GetClockInfo(0)).Returns((1600, 8000));

        var state = _control.GetState();

        Assert.Equal(60, state.Temperature);
        Assert.Equal(75, state.HotspotTemperature);
        Assert.Equal(48, state.Usage);
        Assert.Equal(180, state.Power);
        Assert.Equal(2048, state.UsedVramMB);
        Assert.Equal(8192, state.TotalVramMB);
        Assert.Equal(1600, state.CoreClockMHz);
        Assert.Equal(8000, state.MemoryClockMHz);
    }

    [Fact]
    public void GetState_WhenApiReturnsNulls_UsesDefaults()
    {
        _mockAdl2.Setup(n => n.GetTemperature(0)).Returns((int?)null);
        _mockAdl2.Setup(n => n.GetHotspotTemperature(0)).Returns((int?)null);
        _mockAdl2.Setup(n => n.GetUsage(0)).Returns((int?)null);
        _mockAdl2.Setup(n => n.GetPower(0)).Returns((float?)null);
        _mockAdl2.Setup(n => n.GetVramInfo(0)).Returns(((long, long)?)null);
        _mockAdl2.Setup(n => n.GetClockInfo(0)).Returns(((int, int)?)null);

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
        var mockAdl2 = new Mock<IAdl2Gpu>();
        mockAdl2.Setup(n => n.IsAvailable).Returns(true);
        mockAdl2.Setup(n => n.GpuCount).Returns(1);

        using var control = new AmdGpuControl(5, mockAdl2.Object);

        Assert.False(control.IsValid);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _control.Dispose();
        _control.Dispose();
    }

    [Fact]
    public void AllSensorMethods_WhenAdl2Throws_ReturnsNull()
    {
        var mockAdl2 = new Mock<IAdl2Gpu>();
        mockAdl2.Setup(n => n.IsAvailable).Returns(true);
        mockAdl2.Setup(n => n.GpuCount).Returns(1);
        mockAdl2.Setup(n => n.GetGpuName(0)).Returns("Test GPU");
        mockAdl2.Setup(n => n.GetTemperature(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetHotspotTemperature(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetUsage(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetVramInfo(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetPower(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetClockInfo(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetFPSLimit(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetiGpuSensors(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.GetFanSpeed(0)).Throws<InvalidOperationException>();

        using var control = new AmdGpuControl(0, mockAdl2.Object);

        Assert.Null(control.GetCurrentTemperature());
        Assert.Null(control.GetHotspotTemperature());
        Assert.Null(control.GetGpuUse());
        Assert.Null(control.GetVramInfo());
        Assert.Null(control.GetGpuPower());
        Assert.Null(control.GetClockInfo());
        Assert.Null(control.GetFPSLimit());
        Assert.Null(control.GetiGpuSensors());
        Assert.Null(control.GetFanSpeed());
    }

    [Fact]
    public void AllControlMethods_WhenAdl2Throws_ReturnsFalse()
    {
        var mockAdl2 = new Mock<IAdl2Gpu>();
        mockAdl2.Setup(n => n.IsAvailable).Returns(true);
        mockAdl2.Setup(n => n.GpuCount).Returns(1);
        mockAdl2.Setup(n => n.GetGpuName(0)).Returns("Test GPU");
        mockAdl2.Setup(n => n.SetClocks(0, 0, 0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.ResetClocks(0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.SetPowerLimit(0, 0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.SetFPSLimit(0, 0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.SetiGpuPower(0, 0)).Throws<InvalidOperationException>();
        mockAdl2.Setup(n => n.SetFanSpeed(0, 0)).Throws<InvalidOperationException>();

        using var control = new AmdGpuControl(0, mockAdl2.Object);

        Assert.False(control.SetClocks(0, 0));
        Assert.False(control.ResetClocks());
        Assert.False(control.SetPowerLimit(0));
        Assert.False(control.SetFPSLimit(0));
        Assert.False(control.SetiGpuPower(0));
        Assert.False(control.SetFanSpeed(0));
    }
}